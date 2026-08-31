namespace DevComputaria.Prompts.Catalogs;

public sealed class PromptManifestConsistencyException : Exception
{
    public PromptManifestConsistencyException(IEnumerable<string> issues)
        : base(BuildMessage(issues))
    {
        Issues = Normalize(issues);
    }

    public IReadOnlyList<string> Issues { get; }

    private static string BuildMessage(IEnumerable<string> issues)
    {
        var normalized = Normalize(issues);
        return $"Prompt manifest/resource consistency validation failed: {string.Join(" | ", normalized)}";
    }

    private static IReadOnlyList<string> Normalize(IEnumerable<string> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);

        var normalized = issues
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        if (normalized.Length == 0)
        {
            throw new ArgumentException("At least one consistency issue must be provided.", nameof(issues));
        }

        return normalized;
    }
}