namespace EditorLearningTask;

public sealed class Editor(CodeModel codeModel, Colorizer colorizer, IWriter writer) : IDisposable
{
    public void Initialize(string filePath)
    {
        codeModel.StartLoadingFrom(filePath);
    }

    public async Task Display(int startLine, int count)
    {
        await codeModel.EnsureLineLoaded(startLine + count);

        var codeLines = codeModel.GetLines(startLine, count);
        foreach (var codeLine in codeLines)
        {
            foreach (var token in codeLine.Tokens)
            {
                writer.Write(colorizer.GetColor(token), token.Text);
            }
            writer.WriteLine();
        }
    }

    public Task TokenizeAll() => codeModel.EnsureFullyLoaded();

    public void Dispose() => codeModel.Dispose();
}