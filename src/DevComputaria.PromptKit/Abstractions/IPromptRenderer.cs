namespace DevComputaria.PromptKit.Abstractions;

public interface IPromptRenderer
{
    ValueTask<RenderedPrompt> RenderAsync(PromptId id, PromptArgs args, CancellationToken cancellationToken = default);
}
