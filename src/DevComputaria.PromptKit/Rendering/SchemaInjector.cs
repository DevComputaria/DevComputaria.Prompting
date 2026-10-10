using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Json.Nodes;
using DevComputaria.PromptKit.Abstractions;

namespace DevComputaria.PromptKit.Rendering;

public sealed class SchemaInjector
{
    public PromptSpec Inject(PromptSpec prompt, Func<string, string> schemaResolver)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentNullException.ThrowIfNull(schemaResolver);

        if (prompt.OutputInjectAs is null)
        {
            return prompt;
        }

        if (prompt.OutputSchemaRef is null)
        {
            throw new InvalidOperationException($"Prompt '{prompt.Id}' declares output.inject_as without output.schema_ref.");
        }

        if (!string.Equals(prompt.OutputInjectFormat, "compact_example", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Prompt '{prompt.Id}' uses unsupported output.inject_format '{prompt.OutputInjectFormat}'.");
        }

        var schema = JsonNode.Parse(schemaResolver(prompt.OutputSchemaRef))
                     ?? throw new InvalidOperationException($"Output schema '{prompt.OutputSchemaRef}' is empty.");
        var example = BuildExample(schema)
                      ?? throw new InvalidOperationException($"Output schema '{prompt.OutputSchemaRef}' cannot produce a compact example.");
        var injections = prompt.InjectedArguments.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
        injections[prompt.OutputInjectAs] = example.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = false,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        return Copy(prompt, injectedArguments: injections);
    }

    private static JsonNode? BuildExample(JsonNode schema)
    {
        if (schema["example"] is JsonNode example)
        {
            return example.DeepClone();
        }

        if (schema["default"] is JsonNode defaultValue)
        {
            return defaultValue.DeepClone();
        }

        if (schema["enum"] is JsonArray enumValues && enumValues.Count > 0)
        {
            return enumValues[0]?.DeepClone();
        }

        return schema["type"]?.GetValue<string>() switch
        {
            "object" => BuildObjectExample(schema),
            "array" => new JsonArray(),
            "string" => JsonValue.Create("<string>"),
            "integer" => JsonValue.Create(0),
            "number" => JsonValue.Create(0),
            "boolean" => JsonValue.Create(false),
            _ => null
        };
    }

    private static JsonObject BuildObjectExample(JsonNode schema)
    {
        var result = new JsonObject();
        if (schema["properties"] is not JsonObject properties)
        {
            return result;
        }

        foreach (var property in properties)
        {
            result[property.Key] = property.Value is null ? null : BuildExample(property.Value);
        }

        return result;
    }

    internal static PromptSpec Copy(
        PromptSpec prompt,
        IEnumerable<RenderedMessage>? parts = null,
        IEnumerable<KeyValuePair<string, string>>? injectedArguments = null,
        IEnumerable<PromptTool>? tools = null)
        => new(
            prompt.Id,
            parts ?? prompt.Parts,
            prompt.Variables.Values,
            prompt.Includes,
            prompt.OutputSchemaRef,
            prompt.OutputInjectAs,
            prompt.OutputInjectFormat,
            prompt.Skills,
            injectedArguments ?? prompt.InjectedArguments,
            tools ?? prompt.Tools);
}
