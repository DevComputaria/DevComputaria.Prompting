using DevComputaria.PromptKit.Abstractions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DevComputaria.Prompts.Catalogs;

public sealed class YamlPromptLoader
{
    private readonly IDeserializer _deserializer;

    public YamlPromptLoader()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    public PromptSpec Load(string yaml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(yaml);

        var model = _deserializer.Deserialize<PromptFileModel>(yaml)
                    ?? throw new InvalidOperationException("Prompt YAML could not be deserialized.");

        return model.ToPromptSpec();
    }

    public PromptSpec Load(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var reader = new StreamReader(stream, leaveOpen: true);
        var yaml = reader.ReadToEnd();
        stream.Position = 0;
        return Load(yaml);
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