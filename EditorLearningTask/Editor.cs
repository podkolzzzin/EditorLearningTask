namespace EditorLearningTask;

public sealed class Editor(Lexer lexer, Colorizer colorizer, Reader reader) : IDisposable
{
    public void Initialize(string filePath)
    {
        reader.Open(filePath);
        reader.StartBackgroundIndexing();
    }

    public void Display(int startLine, int count)
    {
        reader.EnsureLineIsIndexed(startLine + count - 1);

        // Tokenize from a clean start so multi-line comment state is correct.
        int cleanStart = reader.FindCleanStartLine(startLine);
        var lines = reader.ReadLines(cleanStart, startLine + count - cleanStart);
        var tokenLines = lexer.Tokenize(lines);

        int displayFrom = startLine - cleanStart;
        for (int i = displayFrom; i < tokenLines.Count; i++)
        {
            foreach (var token in tokenLines[i])
            {
                Console.ForegroundColor = colorizer.GetColor(token);
                Console.Write(token.Text);
            }
            Console.WriteLine();
        }
        Console.ResetColor();
    }

    // Tokenize the whole file in batches without producing any output.
    // Intended for benchmarking — results are discarded between batches.
    public void TokenizeAll()
    {
        const int batchSize = 10_000;
        int total = reader.IndexedLineCount;
        for (int start = 0; start < total; start += batchSize)
        {
            int count = Math.Min(batchSize, total - start);
            if (count <= 0)
            {
                return;
            }
            var lines = reader.ReadLines(start, count);
            lexer.Tokenize(lines);
        }
    }

    public void Edit()
    {
        // ensure file is fully loaded and tokenized before allowing edits
        reader.WaitForFullIndexing();
    }

    public void Dispose() => reader.Dispose();
}