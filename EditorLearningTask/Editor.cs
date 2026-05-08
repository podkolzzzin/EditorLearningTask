namespace EditorLearningTask;

public sealed class Editor(Lexer lexer, Colorizer colorizer, Reader reader, IWriter writer) : IDisposable
{
    public void Initialize(string filePath)
    {
        reader.Open(filePath);

        var time = new TimeMeasurement().Measure("Time to fully index file");
        reader.StartBackgroundIndexing(onFinishedIndexing: time.Dispose);
    }

    public async Task Display(int startLine, int count)
    {
        await reader.EnsureLineIsIndexed(startLine + count + 1);

        // Tokenize from a clean start so multi-line comment state is correct.
        var cleanStart = reader.FindCleanStartLine(startLine);
        var lines = reader.ReadLines(cleanStart, startLine + count - cleanStart);
        var tokenLines = lexer.Tokenize(lines);

        int displayFrom = startLine - cleanStart;
        for (int i = displayFrom; i < tokenLines.Count; i++)
        {
            foreach (var token in tokenLines[i])
            {
                writer.Write(colorizer.GetColor(token), token.Text);
            }
            writer.WriteLine();
        }
        Console.ResetColor();
    }

    public async Task TokenizeAll()
    {
        // Ensure whole file is indexed
        await reader.EnsureLineIsIndexed(int.MaxValue);

        var lines = reader.ReadLines(startLine: 0, count: reader.GetIndexedLineCount());
        lexer.Tokenize(lines);
    }

    public void Dispose() => reader.Dispose();
}