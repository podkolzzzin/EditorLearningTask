using System.Collections.Concurrent;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace EditorLearningTask;

// Reads file via memory-mapped view and incrementally indexes line offsets.
// Indexing can run on a background thread; foreground requests cooperate
// under a single lock so a Display call can race ahead when needed.
public sealed class Reader : IDisposable
{
    private const int ChunkSize = 4 * 1024; // 4 KB
    
    private readonly byte[] _scanBuffer = new byte[ChunkSize];
    private readonly List<long> _lineStarts = [0];
    private readonly ConcurrentDictionary<long, TaskCompletionSource> _lineAwaiters = [];
    private readonly Lock _lock = new();
    private readonly CancellationTokenSource _disposeCancellationTokenSource = new();

    private MemoryMappedFile? _mmf;
    private MemoryMappedViewAccessor? _accessor;
    private Task? _backgroundIndex;
    
    private long _fileSize;
    private long _scannedTo;

    private int IndexedLineCount => _lineStarts.Count;
    private bool IsFullyIndexed => _scannedTo >= _fileSize;

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

    public void StartBackgroundIndexing(Action? onFinishedIndexing = null)
    {
        if (_backgroundIndex is not null)
        {
            return; // Already started
        }
        
        _backgroundIndex = Task.Run(() =>
        {
            while (true)
            {
                if (_disposeCancellationTokenSource.IsCancellationRequested)
                {
                    return;
                }
                
                lock (_lock)
                {
                    if (IsFullyIndexed)
                    {
                        foreach (var lineAwaiter in _lineAwaiters)
                        {
                            lineAwaiter.Value.TrySetResult();
                        }
                        _lineAwaiters.Clear();
                        onFinishedIndexing?.Invoke();
                        return;
                    }
                    
                    ScanNextChunk();
                }
            }
        });
    }

    // Wait until line `lineIndex` is indexed with its start position known.
    public Task EnsureLineIsIndexed(int lineIndex)
    {
        lock (_lock)
        {
            if (IndexedLineCount > lineIndex || IsFullyIndexed)
            {
                return Task.CompletedTask;
            }
            
            var lineAwaiter = _lineAwaiters.GetOrAdd(
                key: lineIndex,
                value: new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
            
            return lineAwaiter.Task;
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
            actualCount = Math.Min(count, IndexedLineCount - startLine);
            if (actualCount <= 0)
            {
                return [];
            }
            
            bufferStart = _lineStarts[startLine];
            lineEnds = new long[actualCount];
            for (int i = 0; i < actualCount; i++)
            {
                int index = startLine + i + 1;
                lineEnds[i] = index < IndexedLineCount ? _lineStarts[index] : _fileSize;
            }
        }

        var byteLength = (int)(lineEnds[^1] - bufferStart);
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

        if (targetLine == 0)
        {
            return 0;
        }
        
        long endByte;
        lock (_lock) endByte = _lineStarts[targetLine];

        var buffer = new byte[ChunkSize];
        long position = endByte;
        byte? previousFirst = null;
        while (position > 0)
        {
            long readStart = Math.Max(0, position - ChunkSize);
            int length = (int)(position - readStart);
            _accessor.ReadArray(readStart, buffer, offset: 0, length);

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

    public int GetIndexedLineCount()
    {
        lock (_lock) return IndexedLineCount;
    }
    
    private void ScanNextChunk()
    {
        var toRead = Math.Min(ChunkSize, _fileSize - _scannedTo);
        if (toRead <= 0)
        {
            foreach (var lineAwaiter in _lineAwaiters)
            {
                lineAwaiter.Value.TrySetResult();
            }
            _lineAwaiters.Clear();

            return;
        }

        if (_accessor is null)
        {
            throw new Exception("Accessor not initialized");
        }

        _accessor.ReadArray(_scannedTo, _scanBuffer, offset: 0, (int)toRead);
        for (int i = 0; i < toRead; i++)
        {
            if (_scanBuffer[i] != (byte)'\n')
            {
                continue;
            }
            
            var nextLineStart = _scannedTo + i + 1;
            // Skip the phantom entry past EOF when the file ends with '\n'.
            if (nextLineStart < _fileSize)
            {
                _lineStarts.Add(nextLineStart);
                if (_lineAwaiters.TryRemove(IndexedLineCount, out var lineAwaiter))
                {
                    lineAwaiter.SetResult();
                }
            }
        }
        
        _scannedTo += toRead;
    }


    private int LineAtOrAfter(long offset)
    {
        lock (_lock)
        {
            int low = 0;
            int high = IndexedLineCount;
            
            while (low < high)
            {
                int mid = (low + high) / 2;
                if (_lineStarts[mid] < offset) low = mid + 1;
                else high = mid;
            }
            
            return Math.Min(low, IndexedLineCount - 1);
        }
    }

    public void Dispose()
    {
        _disposeCancellationTokenSource.Cancel();
        try { _backgroundIndex?.Wait(); } catch { }

        _accessor?.Dispose();
        _mmf?.Dispose();
    }
}
