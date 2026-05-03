// Top-level statements for simplified entry point

using EditorLearningTask;

if (args is ["generate", _, ..] && int.TryParse(args[1], out int requestedLines) && requestedLines > 0)
{
    Generator.GenerateSqlFile(requestedLines);
    return;
}

if (args.Length == 1)
{
    var filePath = args[0];
    if (File.Exists(filePath))
    {
        const int LinesPerPage = 8;
        var time = new TimeMeasurement();
        using (time.Measure("Total time"))
        {
            var editor = new Editor(new Lexer(), new Colorizer());
            using (time.Measure("Time to display first page"))
            {
                editor.Initialize(filePath);
                editor.Display(0, LinesPerPage);
            }

            using (time.Measure("Time to display second page"))
                editor.Display(100_000, LinesPerPage);
            editor.Edit();
        }
    }
    else
    {
        Console.WriteLine($"File not found: {filePath}");
    }
    return;
}

public record Token(int Start, int Length, int Value, string Text);

public class Colorizer
{
    public ConsoleColor GetColor(Token token)
    {
        if (SqlTokenTypes.IsReservedKeyword(token.Value))
            return ConsoleColor.Blue;
        if (SqlTokenTypes.IsKeyword(token.Value))
            return ConsoleColor.DarkBlue;
        if (token.Value == SqlTokenTypes.TOKEN_IDENTIFIER)
            return ConsoleColor.DarkYellow;
        if (token.Value == SqlTokenTypes.TOKEN_COMMENT)
            return ConsoleColor.DarkYellow;
        if (token.Value is SqlTokenTypes.TOKEN_STRING or >= SqlTokenTypes.TOKEN_NUMBER)
            return ConsoleColor.DarkRed;
        return ConsoleColor.White;
    }
}

public class Editor(Lexer lexer, Colorizer colorizer)
{
    private List<string> _lines = new();
    private List<List<Token>> _tokenLines = new();


    public void Initialize(string filePath)
    {
        var l = File.ReadAllLines(filePath);
        _lines = new List<string>(l);
        _tokenLines = lexer.Tokenize(l);
    }

    public void Display(int startLine, int count)
    {
        for (int i = startLine; i < startLine + count && i < _lines.Count; i++)
        {
            var tokens = _tokenLines[i];
            foreach (var token in tokens)
            {
                Console.ForegroundColor = colorizer.GetColor(token);
                Console.Write(token.Text);
            }
            Console.WriteLine();
        }
    }

    public void Edit()
    {
        // ensure file is fully loaded and tokenized before allowing edits
    }
}