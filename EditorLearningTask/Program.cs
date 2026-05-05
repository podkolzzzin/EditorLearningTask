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
            var editor = new BaselineStatelessEditor(new Lexer(), new Colorizer(), new StubWriter());
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


public interface IEditor
{
    void Initialize(string filePath);
    void Display(int startLine, int count);
    void Edit();
}