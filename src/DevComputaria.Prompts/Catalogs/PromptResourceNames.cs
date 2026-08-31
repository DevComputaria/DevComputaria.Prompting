namespace DevComputaria.Prompts.Catalogs;

public static class PromptResourceNames
{
    public const string Catalog = "prompts.catalog.yaml";

    public static string PromptFile(string promptId, string version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(promptId);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        var parts = promptId.Trim().Split('.', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            throw new ArgumentException("Prompt id must follow the '{domain}.{slug}' format.", nameof(promptId));
        }

        return $"prompts.{parts[0]}/{parts[1]}/{version.Trim()}.yaml";
    }

    public static bool IsPromptFile(string resourceName)
    {
        return resourceName.StartsWith("prompts.", StringComparison.Ordinal)
               && resourceName.EndsWith(".yaml", StringComparison.Ordinal)
               && !resourceName.Equals(Catalog, StringComparison.Ordinal)
               && !resourceName.StartsWith("prompts._shared/", StringComparison.Ordinal);
    }
}