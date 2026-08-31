using System.Reflection;

namespace DevComputaria.Prompts.Catalogs;

public sealed class PromptManifestConsistencyValidator
{
    public void ValidateOrThrow(Assembly assembly, PromptManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(manifest);

        ValidateOrThrow(manifest, assembly.GetManifestResourceNames());
    }

    public void ValidateOrThrow(PromptManifest manifest, IEnumerable<string> resourceNames)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(resourceNames);

        var issues = Validate(manifest, resourceNames).ToArray();
        if (issues.Length > 0)
        {
            throw new PromptManifestConsistencyException(issues);
        }
    }

    public IReadOnlyList<string> Validate(PromptManifest manifest, IEnumerable<string> resourceNames)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(resourceNames);

        var resources = resourceNames.ToHashSet(StringComparer.Ordinal);
        var expectedEntries = manifest.ExpandVersions().ToArray();
        var expectedResources = expectedEntries
            .Select(x => x.ResourceName)
            .ToHashSet(StringComparer.Ordinal);
        var publishedVersionsById = expectedEntries
            .GroupBy(x => x.Id, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(x => x.Version).ToHashSet(StringComparer.Ordinal),
                StringComparer.Ordinal);

        var issues = new List<string>();

        foreach (var alias in manifest.Aliases.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            if (!publishedVersionsById.TryGetValue(alias.Key, out var versions))
            {
                issues.Add($"Alias '{alias.Key}' points to a prompt id that is not published in the manifest.");
                continue;
            }

            if (!versions.Contains(alias.Value))
            {
                issues.Add($"Alias '{alias.Key}' points to unpublished version '{alias.Value}'.");
            }
        }

        foreach (var entry in expectedEntries.OrderBy(x => x.ResourceName, StringComparer.Ordinal))
        {
            if (!resources.Contains(entry.ResourceName))
            {
                issues.Add($"Manifest entry '{entry.Id}@{entry.Version}' points to missing resource '{entry.ResourceName}'.");
            }
        }

        foreach (var resource in resources.Where(PromptResourceNames.IsPromptFile).OrderBy(x => x, StringComparer.Ordinal))
        {
            if (!expectedResources.Contains(resource))
            {
                issues.Add($"Resource '{resource}' is embedded but not declared in the manifest.");
            }
        }

        return issues;
    }
}