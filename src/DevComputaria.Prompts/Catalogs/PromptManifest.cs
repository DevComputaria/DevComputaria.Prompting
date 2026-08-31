using System.Reflection;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DevComputaria.Prompts.Catalogs;

public sealed class PromptManifest
{
    public PromptManifest(string package, int schema, IReadOnlyDictionary<string, string> aliases, IReadOnlyList<ManifestPromptEntry> prompts)
    {
        Package = string.IsNullOrWhiteSpace(package) ? throw new ArgumentException("Package cannot be null or empty.", nameof(package)) : package.Trim();
        Schema = schema;
        Aliases = aliases;
        Prompts = prompts;
    }

    public string Package { get; }

    public int Schema { get; }

    public IReadOnlyDictionary<string, string> Aliases { get; }

    public IReadOnlyList<ManifestPromptEntry> Prompts { get; }

    public IEnumerable<ManifestPromptVersion> ExpandVersions()
        => Prompts.SelectMany(prompt => prompt.Versions.Select(version => new ManifestPromptVersion(prompt.Id, version, PromptResourceNames.PromptFile(prompt.Id, version))));

    public static PromptManifest Load(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        using var stream = assembly.GetManifestResourceStream(PromptResourceNames.Catalog)
                           ?? throw new InvalidOperationException($"Embedded resource '{PromptResourceNames.Catalog}' was not found.");
        using var reader = new StreamReader(stream);
        return Load(reader.ReadToEnd());
    }

    public static PromptManifest Load(string yaml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(yaml);

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var model = deserializer.Deserialize<ManifestModel>(yaml)
                    ?? throw new InvalidOperationException("Prompt manifest YAML could not be deserialized.");

        return new PromptManifest(
            model.Package,
            model.Schema,
            model.Aliases ?? new Dictionary<string, string>(StringComparer.Ordinal),
            (model.Prompts ?? new List<ManifestPromptEntryModel>())
                .Select(prompt => new ManifestPromptEntry(prompt.Id, prompt.Versions?.ToArray() ?? Array.Empty<string>(), prompt.Tags?.ToArray() ?? Array.Empty<string>()))
                .ToArray());
    }

    public sealed record ManifestPromptEntry(string Id, IReadOnlyList<string> Versions, IReadOnlyList<string> Tags);

    public sealed record ManifestPromptVersion(string Id, string Version, string ResourceName);

    private sealed class ManifestModel
    {
        public string Package { get; set; } = string.Empty;

        public int Schema { get; set; }

        public Dictionary<string, string>? Aliases { get; set; }

        public List<ManifestPromptEntryModel>? Prompts { get; set; }
    }

    private sealed class ManifestPromptEntryModel
    {
        public string Id { get; set; } = string.Empty;

        public List<string>? Versions { get; set; }

        public List<string>? Tags { get; set; }
    }
}