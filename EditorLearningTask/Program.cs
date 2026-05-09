if (args is ["generate", _, ..] && int.TryParse(args[1], out int requestedLines) && requestedLines > 0)
{
    Generator.GenerateSqlFile(requestedLines);
}

if (File.Exists(Generator.FileName))
{
    const int linesPerPage = 30;
    var editor = new Editor(new CodeModel(new Reader(), new Tokenizer(new Lexer())), new Colorizer(), new StubWriter());
    
    var time = new TimeMeasurement();
    using (time.Measure("Total time"))
    {
        using (time.Measure("Time to display the first page"))
        {
            editor.Initialize(Generator.FileName);
            await editor.Display(0, linesPerPage);
        }

        using (time.Measure("Time to display the second page"))
            await editor.Display(100_000, linesPerPage);

        using (time.Measure("Time to display the third page"))
            await editor.Display(1_000_000, linesPerPage);

        using (time.Measure("Time to tokenize the rest of the file"))
            await editor.TokenizeAll();
    }
}
else
{
    Console.WriteLine($"File not found: {Generator.FileName}");
}