using System.Threading.Channels;

namespace EditorLearningTask;

public sealed record CodeLine(int Index, string Text, IReadOnlyList<Token> Tokens);

// Sink of the pipeline. Stores tokenized lines as they arrive from the
// Tokenizer stage. Consumers (editor, search, ...) read from here.
// Pipeline:   File -> Reader -> [lines]   -> Tokenizer -> [code lines] -> CodeModel
public sealed class CodeModel(Reader reader, Tokenizer tokenizer) : IDisposable
{
    private readonly List<CodeLine?> _codeLines = [];
    private readonly Dictionary<int, TaskCompletionSource> _lineAwaiters = [];
    private readonly Lock _lock = new();
    private readonly CancellationTokenSource _cts = new();

    private Task? _pipeline;
    private bool _isCompleted;

    public void StartLoadingFrom(string filePath, Action? onFinishedLoading = null)
    {
        const int channelCapacity = 1_000_000;
        
        var lineChannel = Channel.CreateBounded<LineItem>(channelCapacity);
        var tokenChannel = Channel.CreateBounded<CodeLine>(channelCapacity);

        var time = new TimeMeasurement().Measure("Time to fully index lines in file");
        reader.Open(filePath);
        var producerLines = reader.StartBackgroundLineIndexing(lineChannel.Writer, time.Dispose);
        var producerTokens = tokenizer.ProduceTokens(lineChannel.Reader, tokenChannel.Writer, _cts.Token);
        var sink = ConsumeTokens(tokenChannel.Reader, _cts.Token);

        _pipeline = Task.Run(async () =>
        {
            try
            {
                await Task.WhenAll(producerLines, producerTokens, sink);
            }
            finally
            {
                List<TaskCompletionSource> lineAwaiters;
                lock (_lock)
                {
                    _isCompleted = true;
                    lineAwaiters = [.._lineAwaiters.Values];
                    _lineAwaiters.Clear();
                }
                foreach (var awaiter in lineAwaiters)
                {
                    awaiter.TrySetResult();
                }
                
                onFinishedLoading?.Invoke();
            }
        }, _cts.Token);
    }

    private async Task ConsumeTokens(ChannelReader<CodeLine> channelReader, CancellationToken ct)
    {
        await foreach (var line in channelReader.ReadAllAsync(ct))
        {
            lock (_lock)
            {
                while (_codeLines.Count <= line.Index)
                {
                    _codeLines.Add(null);
                }
                _codeLines[line.Index] = line;
                
                if (_lineAwaiters.Remove(line.Index, out var lineAwaiter))
                {
                    lineAwaiter.TrySetResult();
                }
            }
        }
    }

    // Resolves when code line with specified index exists in the model
    public Task EnsureLineLoaded(int lineIndex)
    {
        lock (_lock)
        {
            if (_isCompleted || (lineIndex < _codeLines.Count && _codeLines[lineIndex] is not null))
            {
                return Task.CompletedTask;
            }

            if (!_lineAwaiters.TryGetValue(lineIndex, out var lineAwaiter))
            {
                lineAwaiter = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                _lineAwaiters[lineIndex] = lineAwaiter;
            }
            
            return lineAwaiter.Task;
        }
    }

    public CodeLine[] GetLines(int startLine, int count)
    {
        lock (_lock)
        {
            var actualLinesCount = Math.Min(count, Math.Max(0, _codeLines.Count - startLine));
            var lines = new CodeLine[actualLinesCount];
            for (int i = 0; i < actualLinesCount; i++)
            {
                lines[i] = _codeLines[startLine + i]!;
            }
            
            return lines;
        }
    }

    public Task EnsureFullyLoaded() => _pipeline ?? Task.CompletedTask;

    public void Dispose()
    {
        _cts.Cancel();
        try { _pipeline?.Wait(); } catch { /* ignore exceptions when disposing */ }
        _cts.Dispose();
    }
}
