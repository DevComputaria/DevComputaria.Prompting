using DevComputaria.PromptKit.Abstractions;

namespace DevComputaria.PromptKit.Exceptions;

public sealed class MissingRequiredVariableException : Exception
{
    public MissingRequiredVariableException(PromptId promptId, IEnumerable<string> missingVariables)
        : base(BuildMessage(promptId, missingVariables))
    {
        PromptId = promptId;
        MissingVariables = Normalize(missingVariables);
    }

    public PromptId PromptId { get; }

    public IReadOnlyList<string> MissingVariables { get; }

    private static string BuildMessage(PromptId promptId, IEnumerable<string> missingVariables)
    {
        var normalized = Normalize(missingVariables);
        return $"Prompt '{promptId}' is missing required variable(s): {string.Join(", ", normalized)}.";
    }

    private static IReadOnlyList<string> Normalize(IEnumerable<string> missingVariables)
    {
        ArgumentNullException.ThrowIfNull(missingVariables);

        var normalized = missingVariables
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        if (normalized.Length == 0)
        {
            throw new ArgumentException("At least one missing variable must be provided.", nameof(missingVariables));
        }

        return normalized;
    }
}