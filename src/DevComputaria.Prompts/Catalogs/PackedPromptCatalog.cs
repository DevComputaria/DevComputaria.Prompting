using System.Reflection;
using DevComputaria.PromptKit.Abstractions;
using DevComputaria.PromptKit.Catalogs;

namespace DevComputaria.Prompts.Catalogs;

public sealed class PackedPromptCatalog : IPromptCatalog
{
    private readonly InMemoryPromptCatalog _inner;
    private readonly PromptManifest _manifest;
    private readonly PromptManifestConsistencyValidator _consistencyValidator;

    public PackedPromptCatalog(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        _consistencyValidator = new PromptManifestConsistencyValidator();
        _manifest = PromptManifest.Load(assembly);
        _consistencyValidator.ValidateOrThrow(assembly, _manifest);
        _inner = new InMemoryPromptCatalog(LoadPrompts(assembly, _manifest, new YamlPromptLoader()));
    }

    public ValueTask<PromptSpec> GetAsync(PromptId id, CancellationToken cancellationToken = default)
        => _inner.GetAsync(id, cancellationToken);

    public IReadOnlyList<PromptManifest.ManifestPromptVersion> GetManifestEntries()
        => _manifest.ExpandVersions().ToArray();

    private static IEnumerable<PromptSpec> LoadPrompts(Assembly assembly, PromptManifest manifest, YamlPromptLoader loader)
    {
        var availableResources = assembly.GetManifestResourceNames().ToHashSet(StringComparer.Ordinal);

        foreach (var entry in manifest.ExpandVersions().OrderBy(x => x.ResourceName, StringComparer.Ordinal))
        {
            if (!availableResources.Contains(entry.ResourceName))
            {
                throw new InvalidOperationException($"Embedded resource '{entry.ResourceName}' declared for '{entry.Id}@{entry.Version}' was not found.");
            }

            using var stream = assembly.GetManifestResourceStream(entry.ResourceName)
                               ?? throw new InvalidOperationException($"Embedded resource '{entry.ResourceName}' was not found.");
            var prompt = loader.Load(stream);

            yield return prompt;
        }
    }
}