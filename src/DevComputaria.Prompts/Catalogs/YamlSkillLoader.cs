using System.Collections;
using System.Text.Json;
using DevComputaria.PromptKit.Abstractions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DevComputaria.Prompts.Catalogs;

public sealed class YamlSkillLoader
{
    private readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .WithAttemptingUnquotedStringTypeDeserialization()
        .IgnoreUnmatchedProperties()
        .Build();

    public SkillSpec Load(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var reader = new StreamReader(stream, leaveOpen: true);
        return Load(reader.ReadToEnd());
    }

    public SkillSpec Load(string yaml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(yaml);
        var model = _deserializer.Deserialize<SkillModel>(yaml)
                    ?? throw new InvalidOperationException("Skill YAML could not be deserialized.");

        return new SkillSpec(
            model.Id,
            model.Version,
            model.Body,
            model.Tools?.Select(tool => new PromptTool(
                tool.Name,
                tool.Description,
                JsonSerializer.Serialize(Normalize(tool.InputSchema)))));
    }

    private static object? Normalize(object? value)
        => value switch
        {
            IDictionary dictionary => dictionary.Keys.Cast<object>().ToDictionary(
                key => Convert.ToString(key) ?? string.Empty,
                key => Normalize(dictionary[key]),
                StringComparer.Ordinal),
            IEnumerable sequence when value is not string => sequence.Cast<object?>().Select(Normalize).ToArray(),
            _ => value
        };

    private sealed class SkillModel
    {
        public string Id { get; set; } = string.Empty;

        public string Version { get; set; } = string.Empty;

        public string? Body { get; set; }

        public List<ToolModel>? Tools { get; set; }
    }

    private sealed class ToolModel
    {
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [YamlMember(Alias = "input_schema")]
        public object? InputSchema { get; set; }
    }
}
