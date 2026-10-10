using System.Text.Json;

namespace DevComputaria.Prompts.Eval.Tests;

internal sealed class PromptEvalCaseDocument
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public string PromptId { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public List<PromptEvalCase> Cases { get; set; } = new();

    public static PromptEvalCaseDocument Load(string path)
        => JsonSerializer.Deserialize<PromptEvalCaseDocument>(File.ReadAllText(path), SerializerOptions)
           ?? throw new InvalidOperationException($"Eval file '{path}' could not be deserialized.");
}

internal sealed class PromptEvalCase
{
    public string Id { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public Dictionary<string, JsonElement> Args { get; set; } = new(StringComparer.Ordinal);

    public List<string> RenderMustContain { get; set; } = new();

    public string CandidateOutput { get; set; } = string.Empty;

    public List<string> OutputMustContain { get; set; } = new();

    public List<string> OutputMustNotContain { get; set; } = new();

    public object? GetArgument(string name)
        => Args[name].ValueKind switch
        {
            JsonValueKind.String => Args[name].GetString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when Args[name].TryGetInt64(out var integer) => integer,
            JsonValueKind.Number => Args[name].GetDecimal(),
            JsonValueKind.Null => null,
            _ => Args[name].GetRawText()
        };
}
