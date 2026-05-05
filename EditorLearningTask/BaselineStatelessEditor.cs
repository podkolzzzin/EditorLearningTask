using EditorLearningTask;

public class BaselineStatelessEditor(Lexer lexer, Colorizer colorizer, IConsoleWriter writer) : IEditor
{
    private string _filePath = "";

    public void Initialize(string filePath)
    {
        _filePath = filePath;
    }

    public void Display(int startLine, int count)
    {
        using var reader = new StreamReader(_filePath);
        lexer.Reset();
        int target = startLine + count;
        string? line;
        for (int idx = 0; idx < target && (line = reader.ReadLine()) is not null; idx++)
        {
            bool emit = idx >= startLine;
            int pos = 0;
            int start = 0;
            while (lexer.NextToken(line, ref pos, out int type))
            {
                if (emit)
                    writer.Write(colorizer.GetColor(type), line.AsSpan(start, pos - start));
                start = pos;
            }
            if (emit) writer.WriteLine();
        }
    }

    public void Edit()
    {
        // baseline editor does not retain state; nothing to finalize
    }
}