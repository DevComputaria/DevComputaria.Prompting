using DevComputaria.PromptKit.Abstractions;

namespace DevComputaria.PromptKit.Exceptions;

public abstract class PromptCatalogException : Exception
{
    protected PromptCatalogException(PromptId requestedPromptId, string message)
        : base(message)
    {
        RequestedPromptId = requestedPromptId;
    }

    public PromptId RequestedPromptId { get; }
}