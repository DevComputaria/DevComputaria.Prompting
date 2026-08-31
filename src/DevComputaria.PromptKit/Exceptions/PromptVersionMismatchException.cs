using DevComputaria.PromptKit.Abstractions;

namespace DevComputaria.PromptKit.Exceptions;

public sealed class PromptVersionMismatchException : PromptCatalogException
{
    public PromptVersionMismatchException(PromptId requestedPromptId, IEnumerable<string> availableVersions)
        : base(
            requestedPromptId,
            $"Prompt '{requestedPromptId}' was not found with the requested version. Available versions for '{requestedPromptId.Name}': {FormatVersions(availableVersions)}.")
    {
        ArgumentNullException.ThrowIfNull(availableVersions);

        AvailableVersions = availableVersions
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyList<string> AvailableVersions { get; }

    private static string FormatVersions(IEnumerable<string> availableVersions)
    {
        ArgumentNullException.ThrowIfNull(availableVersions);

        var versions = availableVersions
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        return versions.Length == 0 ? "<none>" : string.Join(", ", versions);
    }
}