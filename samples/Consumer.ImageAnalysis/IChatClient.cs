using DevComputaria.PromptKit.Abstractions;

namespace Consumer.ImageAnalysis;

internal interface IChatClient
{
    ValueTask<ChatCompletion> CompleteAsync(
        RenderedPrompt prompt,
        CancellationToken cancellationToken = default);
}

internal sealed record ChatCompletion(string Provider, string Content);
