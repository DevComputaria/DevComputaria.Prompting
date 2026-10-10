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

    public static string SkillFile(string skillId, string version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skillId);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        var parts = skillId.Trim().Split('.', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            throw new ArgumentException("Skill id must follow the '{domain}.{slug}' format.", nameof(skillId));
        }

        return $"skills.{parts[0]}/{parts[1]}/{version.Trim()}.yaml";
    }

    public static bool IsSkillFile(string resourceName)
        => resourceName.StartsWith("skills.", StringComparison.Ordinal)
           && resourceName.EndsWith(".yaml", StringComparison.Ordinal);

    public static string OutputSchemaFile(string schemaRef)
    {
        const string prefix = "schemas/output/";
        if (!schemaRef.StartsWith(prefix, StringComparison.Ordinal) || schemaRef.Contains("..", StringComparison.Ordinal))
        {
            throw new ArgumentException("Output schema reference must be under 'schemas/output/'.", nameof(schemaRef));
        }

        return "schemas.output." + schemaRef[prefix.Length..];
    }
}
