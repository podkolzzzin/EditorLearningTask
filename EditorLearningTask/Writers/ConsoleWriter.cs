namespace EditorLearningTask.Writers;

public class ConsoleWriter : IWriter
{
    public void Write(ConsoleColor color, ReadOnlySpan<char> text)
    {
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ResetColor();
    }

    public void WriteLine() => Console.WriteLine();
}