using DevComputaria.PromptKit.Abstractions;

namespace DevComputaria.PromptKit.Exceptions;

public sealed class PromptNotFoundException : PromptCatalogException
{
    public PromptNotFoundException(PromptId requestedPromptId)
        : base(requestedPromptId, $"Prompt '{requestedPromptId}' was not found in the catalog.")
    {
    }
}