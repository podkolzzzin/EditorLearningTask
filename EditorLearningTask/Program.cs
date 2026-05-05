using EditorLearningTask;

if (args is ["generate", _, ..] && int.TryParse(args[1], out int requestedLines) && requestedLines > 0)
{
    Generator.GenerateSqlFile(requestedLines);
}

var filePath = "output.sql";

if (File.Exists(filePath))
{
    const int linesPerPage = 30;
    var time = new TimeMeasurement();
    using (time.Measure("Total time"))
    {
        var reader = new Reader();
        var editor = new Editor(new Lexer(), new Colorizer(), reader);

        editor.Initialize(filePath);

        using (time.Measure("Time to fully index file"))
            reader.WaitForFullIndexing();

        using (time.Measure("Time to display first page"))
            editor.Display(0, linesPerPage);

        using (time.Measure("Time to display second page"))
            editor.Display(100_000, linesPerPage);

        using (time.Measure("Time to display third page"))
            editor.Display(1_000_000, linesPerPage);

        using (time.Measure("Time to tokenize whole file"))
            editor.TokenizeAll();
    }
}
else
{
    Console.WriteLine($"File not found: {filePath}");
}