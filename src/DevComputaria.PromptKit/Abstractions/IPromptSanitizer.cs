namespace DevComputaria.PromptKit.Abstractions;

public interface IPromptSanitizer
{
    ValueTask<PromptArgs> SanitizeAsync(PromptSpec prompt, PromptArgs args, CancellationToken cancellationToken = default);
}
