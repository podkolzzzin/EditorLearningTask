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
    public void EnsureLineIsIndexed(int lineIndex)
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
    
    public string[] ReadLines(int startLine, int count)
    {
        if (_accessor is null)
        {
            throw new Exception("Accessor not initialized");
        }
        
        long bufferStart;
        int actualCount;
        long[] lineEnds;
        lock (_lock)
        {
            actualCount = Math.Min(count, _lineStarts.Count - startLine);
            if (actualCount <= 0)
            {
                return [];
            }
            
            bufferStart = _lineStarts[startLine];
            lineEnds = new long[actualCount];
            for (int i = 0; i < actualCount; i++)
            {
                int index = startLine + i + 1;
                lineEnds[i] = index < _lineStarts.Count ? _lineStarts[index] : _fileSize;
            }
        }

        int byteLength = (int)(lineEnds[^1] - bufferStart);
        var buffer = new byte[byteLength];
        _accessor.ReadArray(bufferStart, buffer, offset: 0, byteLength);

        var result = new string[actualCount];
        
        long lineStartAbs = bufferStart;
        for (int i = 0; i < actualCount; i++)
        {
            int relativeStart = (int)(lineStartAbs - bufferStart);
            int length = (int)(lineEnds[i] - lineStartAbs);
            if (length > 0 && buffer[relativeStart + length - 1] == (byte)'\n') length--;
            if (length > 0 && buffer[relativeStart + length - 1] == (byte)'\r') length--;
            result[i] = Encoding.UTF8.GetString(buffer, relativeStart, length);
            lineStartAbs = lineEnds[i];
        }
        return result;
    }

    /// <summary>
    /// Earliest line we can tokenize from to reach `targetLine` with a correct
    /// multi-line comment state. Scans bytes backward for "*/", which closes
    /// any pending block comment; the line right after is a clean entry point.
    /// </summary>
    public int FindCleanStartLine(int targetLine)
    {
        if (_accessor is null)
        {
            throw new Exception("Accessor not initialized");
        }
        
        long endByte;
        lock (_lock) endByte = _lineStarts[targetLine];
        if (endByte == 0)
        {
            return 0;
        }

        var buffer = new byte[ChunkSize];
        long position = endByte;
        byte? previousFirst = null;
        while (position > 0)
        {
            long readStart = Math.Max(0, position - ChunkSize);
            int length = (int)(position - readStart);
            _accessor.ReadArray(readStart, buffer, 0, length);

            if (previousFirst.HasValue && buffer[length - 1] == (byte)'*' && previousFirst.Value == (byte)'/')
            {
                return LineAtOrAfter(readStart + length + 1);
            }

            for (int i = length - 2; i >= 0; i--)
            {
                if (buffer[i] == (byte)'*' && buffer[i + 1] == (byte)'/')
                {
                    return LineAtOrAfter(readStart + i + 2);
                }
            }
            previousFirst = buffer[0];
            position = readStart;
        }
        
        return 0;
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


    private int LineAtOrAfter(long offset)
    {
        lock (_lock)
        {
            int low = 0;
            int high = _lineStarts.Count;
            
            while (low < high)
            {
                int mid = (low + high) / 2;
                if (_lineStarts[mid] < offset) low = mid + 1;
                else high = mid;
            }
            
            return Math.Min(low, _lineStarts.Count - 1);
        }
    }

    public void Dispose()
    {
        _accessor?.Dispose();
        _mmf?.Dispose();
    }
}
