using DevComputaria.PromptKit.Abstractions;

namespace DevComputaria.PromptKit.Composition;

public sealed class PassthroughPromptComposer : IPromptComposer
{
    public ValueTask<PromptSpec> ComposeAsync(PromptSpec prompt, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(prompt);
    }
}