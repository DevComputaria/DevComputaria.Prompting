using DevComputaria.PromptKit.Abstractions;

namespace DevComputaria.PromptKit.Exceptions;

public sealed class UnsafeTemplateHelperException : Exception
{
    public UnsafeTemplateHelperException(PromptId promptId, string helperName)
        : base($"Prompt '{promptId}' uses blocked helper '{ValidateHelperName(helperName)}'.")
    {
        PromptId = promptId;
        HelperName = helperName.Trim();
    }

    public PromptId PromptId { get; }

    public string HelperName { get; }

    private static string ValidateHelperName(string helperName)
    {
        if (string.IsNullOrWhiteSpace(helperName))
        {
            throw new ArgumentException("Helper name cannot be null, empty, or whitespace.", nameof(helperName));
        }

        return helperName.Trim();
    }
}