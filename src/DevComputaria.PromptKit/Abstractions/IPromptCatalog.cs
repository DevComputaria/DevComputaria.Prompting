namespace DevComputaria.PromptKit.Abstractions;

public interface IPromptCatalog
{
    ValueTask<PromptSpec?> GetAsync(PromptId id, CancellationToken cancellationToken = default);
}
