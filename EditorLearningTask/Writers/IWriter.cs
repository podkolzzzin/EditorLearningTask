namespace EditorLearningTask.Writers;

public interface IWriter
{
    void Write(ConsoleColor color, ReadOnlySpan<char> text);
    void WriteLine();
}