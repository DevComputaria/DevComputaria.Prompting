using System.Reflection;
using DevComputaria.PromptKit.Abstractions;
using DevComputaria.PromptKit.Catalogs;
using DevComputaria.PromptKit.Composition;
using DevComputaria.PromptKit.Rendering;

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
        var skills = new PackedSkillCatalog(assembly, _manifest);
        _inner = new InMemoryPromptCatalog(LoadPrompts(assembly, _manifest, new YamlPromptLoader(), skills));
    }

    public ValueTask<PromptSpec> GetAsync(PromptId id, CancellationToken cancellationToken = default)
        => _inner.GetAsync(id, cancellationToken);

    public IReadOnlyList<PromptManifest.ManifestPromptVersion> GetManifestEntries()
        => _manifest.ExpandVersions().ToArray();

    private static IEnumerable<PromptSpec> LoadPrompts(Assembly assembly, PromptManifest manifest, YamlPromptLoader loader, PackedSkillCatalog skills)
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
            var prompt = loader.Load(stream, from => ReadSiblingPart(assembly, entry.ResourceName, from));
            prompt = new SchemaInjector().Inject(prompt, schemaRef => ReadResource(assembly, PromptResourceNames.OutputSchemaFile(schemaRef)));
            prompt = new SkillAttacher().Attach(prompt, skills.Get);

            yield return prompt;
        }
    }

    private static string ReadSiblingPart(Assembly assembly, string promptResourceName, string from)
    {
        if (string.IsNullOrWhiteSpace(from) || !from.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ||
            from.Contains('/', StringComparison.Ordinal) || from.Contains('\\', StringComparison.Ordinal) ||
            from.Contains("..", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Prompt part reference '{from}' must name a sibling Markdown file.");
        }

        var separator = promptResourceName.LastIndexOf('/');
        var resourceName = promptResourceName[..(separator + 1)] + from;
        return ReadResource(assembly, resourceName);
    }

    private static string ReadResource(Assembly assembly, string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName)
                           ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' was not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
