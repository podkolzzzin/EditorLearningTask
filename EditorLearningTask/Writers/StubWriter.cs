namespace EditorLearningTask.Writers;

public class StubWriter : IWriter
{
    public void Write(ConsoleColor color, ReadOnlySpan<char> text) { }

    public void WriteLine() { }
}