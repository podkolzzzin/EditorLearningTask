using System.IO.MemoryMappedFiles;
using System.Text;

namespace EditorLearningTask;

// Reads file via memory-mapped view and incrementally indexes line offsets.
// Indexing can run on a background thread; foreground requests cooperate
// under a single lock so a Display call can race ahead when needed.
public sealed class Reader : IDisposable
{
    private const int ChunkSize = 32 * 1024;
    
    private readonly List<long> _lineStarts = [0];
    private readonly byte[] _scanBuffer = new byte[ChunkSize];
    private readonly Lock _lock = new();

    private MemoryMappedFile? _mmf;
    private MemoryMappedViewAccessor? _accessor;
    private Task? _backgroundIndex;
    
    private long _fileSize;
    private long _scannedTo;

    public bool IsFullyIndexed
    {
        get { lock (_lock) return _scannedTo >= _fileSize; }
    }

    public int IndexedLineCount
    {
        get { lock (_lock) return _lineStarts.Count; }
    }

    public void Open(string filePath)
    {
        _fileSize = new FileInfo(filePath).Length;
        _mmf = MemoryMappedFile.CreateFromFile(
            path: filePath,
            mode: FileMode.Open,
            mapName: null,
            capacity: 0,
            access: MemoryMappedFileAccess.Read);
        _accessor = _mmf.CreateViewAccessor(offset: 0, size: _fileSize, access: MemoryMappedFileAccess.Read);
    }

    public void StartBackgroundIndexing()
    {
        _backgroundIndex = Task.Run(() =>
        {
            while (true)
            {
                lock (_lock)
                {
                    if (_scannedTo >= _fileSize) return;
                    ScanNextChunk();
                }
            }
        });
    }

    // Block until line `lineIndex` is indexed with its end position known.
    public void EnsureLine(int lineIndex)
    {
        while (true)
        {
            lock (_lock)
            {
                if (_lineStarts.Count > lineIndex + 1 || _scannedTo >= _fileSize) return;
                ScanNextChunk();
            }
        }
    }

    private void ScanNextChunk()
    {
        int toRead = (int)Math.Min(ChunkSize, _fileSize - _scannedTo);
        if (toRead <= 0)
        {
            return;
        }
        
        _accessor!.ReadArray(_scannedTo, _scanBuffer, offset: 0, toRead);
        long basePos = _scannedTo;
        for (int i = 0; i < toRead; i++)
        {
            if (_scanBuffer[i] == (byte)'\n')
                _lineStarts.Add(basePos + i + 1);
        }
        _scannedTo += toRead;
    }

    public string[] ReadLines(int startLine, int count)
    {
        long bufStart;
        int actualCount;
        long[] lineEnds;
        lock (_lock)
        {
            actualCount = Math.Min(count, _lineStarts.Count - startLine);
            if (actualCount <= 0) return Array.Empty<string>();
            bufStart = _lineStarts[startLine];
            lineEnds = new long[actualCount];
            for (int i = 0; i < actualCount; i++)
            {
                int idx = startLine + i + 1;
                lineEnds[i] = idx < _lineStarts.Count ? _lineStarts[idx] : _fileSize;
            }
        }

        int byteLen = (int)(lineEnds[^1] - bufStart);
        var buf = new byte[byteLen];
        _accessor!.ReadArray(bufStart, buf, offset: 0, byteLen);

        var result = new string[actualCount];
        long lineStartAbs = bufStart;
        for (int i = 0; i < actualCount; i++)
        {
            int relStart = (int)(lineStartAbs - bufStart);
            int len = (int)(lineEnds[i] - lineStartAbs);
            if (len > 0 && buf[relStart + len - 1] == (byte)'\n') len--;
            if (len > 0 && buf[relStart + len - 1] == (byte)'\r') len--;
            result[i] = Encoding.UTF8.GetString(buf, relStart, len);
            lineStartAbs = lineEnds[i];
        }
        return result;
    }

    // Earliest line we can tokenize from to reach `targetLine` with a correct
    // multi-line comment state. Scans bytes backward for "*/", which closes
    // any pending block comment; the line right after is a clean entry point.
    public int FindCleanStartLine(int targetLine)
    {
        long endByte;
        lock (_lock) endByte = _lineStarts[targetLine];
        if (endByte == 0) return 0;

        var buf = new byte[ChunkSize];
        long pos = endByte;
        byte? prevFirst = null;
        while (pos > 0)
        {
            long readStart = Math.Max(0, pos - ChunkSize);
            int len = (int)(pos - readStart);
            _accessor!.ReadArray(readStart, buf, 0, len);

            if (prevFirst.HasValue && buf[len - 1] == (byte)'*' && prevFirst.Value == (byte)'/')
                return LineAtOrAfter(readStart + len + 1);

            for (int i = len - 2; i >= 0; i--)
            {
                if (buf[i] == (byte)'*' && buf[i + 1] == (byte)'/')
                    return LineAtOrAfter(readStart + i + 2);
            }
            prevFirst = buf[0];
            pos = readStart;
        }
        return 0;
    }

    private int LineAtOrAfter(long offset)
    {
        lock (_lock)
        {
            int lo = 0, hi = _lineStarts.Count;
            while (lo < hi)
            {
                int mid = (lo + hi) / 2;
                if (_lineStarts[mid] < offset) lo = mid + 1;
                else hi = mid;
            }
            return Math.Min(lo, _lineStarts.Count - 1);
        }
    }

    public void Dispose()
    {
        _accessor?.Dispose();
        _mmf?.Dispose();
    }
}
