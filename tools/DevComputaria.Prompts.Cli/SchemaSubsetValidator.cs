using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;

namespace DevComputaria.Prompts.Cli;

internal sealed class SchemaSubsetValidator
{
    private readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithAttemptingUnquotedStringTypeDeserialization()
        .Build();

    public IReadOnlyList<string> ValidateYaml(string schemaJson, string yaml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaJson);
        ArgumentException.ThrowIfNullOrWhiteSpace(yaml);

        var schema = JsonNode.Parse(schemaJson) ?? throw new InvalidOperationException("Schema JSON could not be parsed.");
        var instance = ToJsonNode(_deserializer.Deserialize(new StringReader(yaml)));
        var errors = new List<string>();
        ValidateNode(schema, instance, "$", errors);
        return errors;
    }

    private static void ValidateNode(JsonNode schema, JsonNode? instance, string path, List<string> errors)
    {
        var type = schema["type"]?.GetValue<string>();
        if (type is not null && !MatchesType(type, instance))
        {
            errors.Add($"{path}: expected type '{type}'.");
            return;
        }

        switch (type)
        {
            case "object":
                ValidateObject(schema, instance?.AsObject(), path, errors);
                break;
            case "array":
                ValidateArray(schema, instance?.AsArray(), path, errors);
                break;
            case "string":
                ValidateString(schema, instance?.GetValue<string>(), path, errors);
                break;
            case "integer":
                ValidateInteger(schema, instance, path, errors);
                break;
            case "boolean":
                if (instance is not JsonValue boolean || !boolean.TryGetValue<bool>(out _))
                {
                    errors.Add($"{path}: expected boolean.");
                }
                break;
        }
    }

    private static void ValidateObject(JsonNode schema, JsonObject? instance, string path, List<string> errors)
    {
        if (instance is null)
        {
            errors.Add($"{path}: expected object.");
            return;
        }

        var properties = schema["properties"] as JsonObject;
        foreach (var propertyName in schema["required"]?.AsArray().Select(node => node!.GetValue<string>()) ?? [])
        {
            if (!instance.ContainsKey(propertyName))
            {
                errors.Add($"{path}: missing required property '{propertyName}'.");
            }
        }

        if (properties is not null)
        {
            foreach (var property in properties)
            {
                if (instance.TryGetPropertyValue(property.Key, out var child))
                {
                    ValidateNode(property.Value!, child, $"{path}.{property.Key}", errors);
                }
            }
        }

        if (schema["additionalProperties"] is JsonObject additionalSchema)
        {
            var declared = properties?.Select(property => property.Key).ToHashSet(StringComparer.Ordinal) ?? [];
            foreach (var property in instance.Where(property => !declared.Contains(property.Key)))
            {
                ValidateNode(additionalSchema, property.Value, $"{path}.{property.Key}", errors);
            }
        }
    }

    private static void ValidateArray(JsonNode schema, JsonArray? instance, string path, List<string> errors)
    {
        if (instance is null)
        {
            errors.Add($"{path}: expected array.");
            return;
        }

        if (schema["minItems"] is JsonNode minimum && instance.Count < minimum.GetValue<int>())
        {
            errors.Add($"{path}: expected at least {minimum.GetValue<int>()} item(s).");
        }

        if (schema["items"] is JsonNode itemSchema)
        {
            for (var index = 0; index < instance.Count; index++)
            {
                ValidateNode(itemSchema, instance[index], $"{path}[{index}]", errors);
            }
        }
    }

    private static void ValidateString(JsonNode schema, string? value, string path, List<string> errors)
    {
        if (value is null)
        {
            errors.Add($"{path}: expected string.");
            return;
        }

        if (schema["minLength"] is JsonNode minimum && value.Length < minimum.GetValue<int>())
        {
            errors.Add($"{path}: expected minimum length {minimum.GetValue<int>()}.");
        }

        if (schema["pattern"] is JsonNode pattern && !Regex.IsMatch(value, pattern.GetValue<string>()))
        {
            errors.Add($"{path}: value '{value}' does not match pattern '{pattern.GetValue<string>()}'.");
        }

        if (schema["enum"] is JsonArray enumValues)
        {
            var allowed = enumValues.Select(node => node!.GetValue<string>()).ToHashSet(StringComparer.Ordinal);
            if (!allowed.Contains(value))
            {
                errors.Add($"{path}: value '{value}' is not one of [{string.Join(", ", allowed)}].");
            }
        }
    }

    private static void ValidateInteger(JsonNode schema, JsonNode? instance, string path, List<string> errors)
    {
        if (instance is not JsonValue value)
        {
            errors.Add($"{path}: expected integer.");
            return;
        }

        long integer;
        if (value.TryGetValue<long>(out var longValue))
        {
            integer = longValue;
        }
        else if (value.TryGetValue<int>(out var intValue))
        {
            integer = intValue;
        }
        else
        {
            errors.Add($"{path}: expected integer.");
            return;
        }

        if (schema["minimum"] is JsonNode minimum && integer < minimum.GetValue<int>())
        {
            errors.Add($"{path}: expected minimum value {minimum.GetValue<int>()}.");
        }
    }

    private static bool MatchesType(string type, JsonNode? instance)
        => type switch
        {
            "object" => instance is JsonObject,
            "array" => instance is JsonArray,
            "string" => instance is JsonValue value && value.TryGetValue<string>(out _),
            "integer" => instance is JsonValue value && (value.TryGetValue<int>(out _) || value.TryGetValue<long>(out _)),
            "boolean" => instance is JsonValue value && value.TryGetValue<bool>(out _),
            _ => true
        };

    private static JsonNode? ToJsonNode(object? value)
        => value switch
        {
            null => null,
            IDictionary<object, object> dictionary => new JsonObject(dictionary.ToDictionary(
                entry => Convert.ToString(entry.Key)!, entry => ToJsonNode(entry.Value))),
            IDictionary<string, object> dictionary => new JsonObject(dictionary.ToDictionary(
                entry => entry.Key, entry => ToJsonNode(entry.Value))),
            IEnumerable<object> sequence when value is not string => new JsonArray(sequence.Select(ToJsonNode).ToArray()),
            bool boolean => JsonValue.Create(boolean),
            byte or sbyte or short or ushort or int or uint or long or ulong => JsonValue.Create(Convert.ToInt64(value)),
            float or double or decimal => JsonValue.Create(Convert.ToDouble(value)),
            _ => JsonValue.Create(Convert.ToString(value))
        };
}
