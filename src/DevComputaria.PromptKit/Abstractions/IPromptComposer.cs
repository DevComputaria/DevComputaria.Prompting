namespace DevComputaria.PromptKit.Abstractions;

public interface IPromptComposer
{
    ValueTask<PromptSpec> ComposeAsync(PromptSpec prompt, CancellationToken cancellationToken = default);
}
