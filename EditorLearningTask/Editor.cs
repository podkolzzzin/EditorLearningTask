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
        reader.EnsureLine(startLine + count - 1);

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

    public void Edit()
    {
        // ensure file is fully loaded and tokenized before allowing edits
        while (!reader.IsFullyIndexed)
            Thread.Sleep(10);
    }

    public void Dispose() => reader.Dispose();
}