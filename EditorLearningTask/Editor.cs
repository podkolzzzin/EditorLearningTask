using EditorLearningTask;

public class Editor(Lexer lexer, Colorizer colorizer) : IEditor
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
                Console.ForegroundColor = colorizer.GetColor(token.Value);
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