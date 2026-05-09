using System.Threading.Channels;

namespace EditorLearningTask;

// Pipeline stage: consumes raw lines from `input` and produces tokenized
// lines into `output`. Lines are tokenized in batches because the underlying
// Lexer is invoked per-batch (it cannot stream tokens line-by-line while
// preserving multi-line state — state lives only inside one Tokenize call).
// A multi-line token spanning a batch boundary will be highlighted incorrectly
// for a few lines after the boundary; for an SQL editor with short comments
// that's an acceptable trade-off.
public sealed class Tokenizer(Lexer lexer)
{
    private const int BatchSize = 50;

    public async Task ProduceTokens(
        ChannelReader<LineItem> lineItemsReader,
        ChannelWriter<CodeLine> tokensWriter,
        CancellationToken ct)
    {
        try
        {
            var lineItemsBatch = new List<LineItem>(BatchSize);
            await foreach (var lineItem in lineItemsReader.ReadAllAsync(ct))
            {
                lineItemsBatch.Add(lineItem);
                if (lineItemsBatch.Count >= BatchSize)
                {
                    await FlushTokens(lineItemsBatch, tokensWriter, ct);
                    lineItemsBatch.Clear();
                }
            }

            if (lineItemsBatch.Count > 0) // Process last batch
            {
                await FlushTokens(lineItemsBatch, tokensWriter, ct);
            }
        }
        catch (OperationCanceledException) { /* shutdown */ }
        catch (Exception exception)
        {
            tokensWriter.Complete(exception);
        }
        finally
        {
            tokensWriter.TryComplete();
        }
    }

    private async Task FlushTokens(
        List<LineItem> lineItems,
        ChannelWriter<CodeLine> output,
        CancellationToken ct)
    {
        var texts = lineItems.Select(l => l.Text).ToArray();
        var tokens = lexer.Tokenize(texts);

        for (int i = 0; i < tokens.Count; i++)
        {
            await output.WriteAsync(new CodeLine(lineItems[i].Index, texts[i], tokens[i]), ct);
        }
    }
}
