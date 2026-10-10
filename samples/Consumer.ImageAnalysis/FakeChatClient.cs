using System.Text.Json;
using DevComputaria.PromptKit.Abstractions;

namespace Consumer.ImageAnalysis;

internal sealed class FakeChatClient : IChatClient
{
    public ValueTask<ChatCompletion> CompleteAsync(
        RenderedPrompt prompt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        cancellationToken.ThrowIfCancellationRequested();

        if (prompt.Messages.Count == 0)
        {
            throw new InvalidOperationException("The fake client requires at least one rendered message.");
        }

        var content = JsonSerializer.Serialize(new
        {
            documentType = "identity-card",
            country = "BR",
            source = "fake-chat-client",
            promptSha256 = prompt.ContentSha256
        });

        return ValueTask.FromResult(new ChatCompletion("fake", content));
    }
}
