namespace EditorLearningTask
{
public interface IConsoleWriter
{
    void Write(ConsoleColor color, ReadOnlySpan<char> text);
    void WriteLine();
}

public class ConsoleWriter : IConsoleWriter
{
    public void Write(ConsoleColor color, ReadOnlySpan<char> text)
    {
        Console.ForegroundColor = color;
        Console.Out.Write(text);
    }

    public void WriteLine() => Console.WriteLine();
}

public class StubWriter : IConsoleWriter
{
    public void Write(ConsoleColor color, ReadOnlySpan<char> text) { }
    public void WriteLine() { }
}
}
