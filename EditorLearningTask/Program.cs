using EditorLearningTask;
using EditorLearningTask.Writers;

if (args is ["generate", _, ..] && int.TryParse(args[1], out int requestedLines) && requestedLines > 0)
{
    Generator.GenerateSqlFile(requestedLines);
}

if (File.Exists(Generator.FileName))
{
    const int linesPerPage = 30;
    var editor = new Editor(new Lexer(), new Colorizer(), new Reader(), new StubWriter());
    
    var time = new TimeMeasurement();
    using (time.Measure("Total time"))
    {
        using (time.Measure("Time to display first page"))
        {
            editor.Initialize(Generator.FileName);
            editor.Display(0, linesPerPage);
        }

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
    Console.WriteLine($"File not found: {Generator.FileName}");
}