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

    public PromptSpec Load(string yaml, Func<string, string>? partResolver = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(yaml);

        var model = _deserializer.Deserialize<PromptFileModel>(yaml)
                    ?? throw new InvalidOperationException("Prompt YAML could not be deserialized.");

        return model.ToPromptSpec(partResolver);
    }

    public PromptSpec Load(Stream stream, Func<string, string>? partResolver = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var reader = new StreamReader(stream, leaveOpen: true);
        var yaml = reader.ReadToEnd();
        stream.Position = 0;
        return Load(yaml, partResolver);
    }

    private sealed class PromptFileModel
    {
        public string Id { get; set; } = string.Empty;

        public string Version { get; set; } = string.Empty;

        public List<string>? Includes { get; set; }

        public Dictionary<string, VariableModel>? Variables { get; set; }

        public OutputModel? Output { get; set; }

        public List<PartModel> Parts { get; set; } = new();

        public List<SkillModel>? Skills { get; set; }

        public PromptSpec ToPromptSpec(Func<string, string>? partResolver)
        {
            return new PromptSpec(
                new PromptId(Id, Version),
                Parts.Select(part => part.ToRenderedMessage(partResolver)),
                Variables?.Select(variable => new PromptVariableSpec(
                    variable.Key,
                    variable.Value.Required,
                    variable.Value.RedactedInLogs,
                    variable.Value.Type)),
                Includes,
                Output?.SchemaRef,
                Output?.InjectAs,
                Output?.InjectFormat,
                Skills?.Select(skill => new SkillReference(skill.Id, skill.Version, skill.Attach)));
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

        [YamlMember(Alias = "inject_as")]
        public string? InjectAs { get; set; }

        [YamlMember(Alias = "inject_format")]
        public string? InjectFormat { get; set; }
    }

    private sealed class SkillModel
    {
        public string Id { get; set; } = string.Empty;

        public string Version { get; set; } = string.Empty;

        public string Attach { get; set; } = string.Empty;
    }

    private sealed class PartModel
    {
        public string Role { get; set; } = string.Empty;

        public string? Template { get; set; }

        public string? From { get; set; }

        public RenderedMessage ToRenderedMessage(Func<string, string>? partResolver)
        {
            if ((Template is null) == (From is null))
            {
                throw new InvalidOperationException("Each prompt part must declare exactly one of 'template' or 'from'.");
            }

            if (From is null)
            {
                return new RenderedMessage(Role, Template!);
            }

            if (partResolver is null)
            {
                throw new InvalidOperationException($"Prompt part '{From}' requires a content resolver.");
            }

            return new RenderedMessage(Role, partResolver(From), From);
        }
    }
}
