using System.Reflection;
using DevComputaria.PromptKit.Abstractions;
using DevComputaria.PromptKit.Catalogs;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DevComputaria.Prompts.Catalogs;

public sealed class PackedPromptCatalog : IPromptCatalog
{
    private readonly InMemoryPromptCatalog _inner;

    public PackedPromptCatalog(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        _inner = new InMemoryPromptCatalog(LoadPrompts(assembly));
    }

    public ValueTask<PromptSpec> GetAsync(PromptId id, CancellationToken cancellationToken = default)
        => _inner.GetAsync(id, cancellationToken);

    private static IEnumerable<PromptSpec> LoadPrompts(Assembly assembly)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        foreach (var resourceName in assembly.GetManifestResourceNames()
                     .Where(name => name.StartsWith("prompts.", StringComparison.Ordinal))
                     .Where(name => name.EndsWith(".yaml", StringComparison.Ordinal))
                     .Where(name => !name.Equals("prompts.catalog.yaml", StringComparison.Ordinal))
                     .Where(name => !name.StartsWith("prompts._shared.", StringComparison.Ordinal))
                     .OrderBy(name => name, StringComparer.Ordinal))
        {
            using var stream = assembly.GetManifestResourceStream(resourceName)
                               ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' was not found.");
            using var reader = new StreamReader(stream);
            var model = deserializer.Deserialize<PromptFileModel>(reader)
                        ?? throw new InvalidOperationException($"Prompt resource '{resourceName}' could not be deserialized.");

            yield return model.ToPromptSpec();
        }
    }

    private sealed class PromptFileModel
    {
        public string Id { get; set; } = string.Empty;

        public string Version { get; set; } = string.Empty;

        public List<string>? Includes { get; set; }

        public Dictionary<string, VariableModel>? Variables { get; set; }

        public OutputModel? Output { get; set; }

        public List<PartModel> Parts { get; set; } = new();

        public PromptSpec ToPromptSpec()
        {
            return new PromptSpec(
                new PromptId(Id, Version),
                Parts.Select(part => new RenderedMessage(part.Role, part.Template)),
                Variables?.Select(variable => new PromptVariableSpec(
                    variable.Key,
                    variable.Value.Required,
                    variable.Value.RedactedInLogs,
                    variable.Value.Type)),
                Includes,
                Output?.SchemaRef);
        }
    }

    private sealed class VariableModel
    {
        public string? Type { get; set; }

        public bool Required { get; set; }

        [YamlMember(Alias = "redacted_in_logs")]
        public bool RedactedInLogs { get; set; }
    }

    private sealed class OutputModel
    {
        [YamlMember(Alias = "schema_ref")]
        public string? SchemaRef { get; set; }
    }

    private sealed class PartModel
    {
        public string Role { get; set; } = string.Empty;

        public string Template { get; set; } = string.Empty;
    }
}