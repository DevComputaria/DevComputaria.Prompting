using System.Collections.ObjectModel;

namespace DevComputaria.PromptKit.Abstractions;

public sealed record PromptSpec
{
    public PromptSpec(
        PromptId id,
        IEnumerable<RenderedMessage> parts,
        IEnumerable<PromptVariableSpec>? variables = null,
        IEnumerable<string>? includes = null,
        string? outputSchemaRef = null,
        string? outputInjectAs = null,
        string? outputInjectFormat = null,
        IEnumerable<SkillReference>? skills = null,
        IEnumerable<KeyValuePair<string, string>>? injectedArguments = null,
        IEnumerable<PromptTool>? tools = null)
    {
        Id = id;
        Parts = Copy(parts);
        Variables = CopyVariables(variables);
        Includes = CopyStrings(includes);
        OutputSchemaRef = string.IsNullOrWhiteSpace(outputSchemaRef) ? null : outputSchemaRef.Trim();
        OutputInjectAs = string.IsNullOrWhiteSpace(outputInjectAs) ? null : outputInjectAs.Trim();
        OutputInjectFormat = string.IsNullOrWhiteSpace(outputInjectFormat) ? null : outputInjectFormat.Trim();
        Skills = (skills ?? Array.Empty<SkillReference>()).ToArray();
        InjectedArguments = CopyInjectedArguments(injectedArguments);
        Tools = (tools ?? Array.Empty<PromptTool>()).ToArray();
    }

    public PromptId Id { get; }

    public IReadOnlyList<RenderedMessage> Parts { get; }

    public IReadOnlyDictionary<string, PromptVariableSpec> Variables { get; }

    public IReadOnlyList<string> Includes { get; }

    public string? OutputSchemaRef { get; }

    public string? OutputInjectAs { get; }

    public string? OutputInjectFormat { get; }

    public IReadOnlyList<SkillReference> Skills { get; }

    public IReadOnlyDictionary<string, string> InjectedArguments { get; }

    public IReadOnlyList<PromptTool> Tools { get; }

    private static IReadOnlyList<RenderedMessage> Copy(IEnumerable<RenderedMessage> parts)
    {
        ArgumentNullException.ThrowIfNull(parts);

        var list = parts.ToList();
        return list.AsReadOnly();
    }

    private static IReadOnlyDictionary<string, PromptVariableSpec> CopyVariables(IEnumerable<PromptVariableSpec>? variables)
    {
        if (variables is null)
        {
            return new ReadOnlyDictionary<string, PromptVariableSpec>(new Dictionary<string, PromptVariableSpec>(StringComparer.Ordinal));
        }

        var map = new Dictionary<string, PromptVariableSpec>(StringComparer.Ordinal);
        foreach (var variable in variables)
        {
            ArgumentNullException.ThrowIfNull(variable);
            map[variable.Name] = variable;
        }

        return new ReadOnlyDictionary<string, PromptVariableSpec>(map);
    }

    private static IReadOnlyList<string> CopyStrings(IEnumerable<string>? values)
    {
        if (values is null)
        {
            return Array.Empty<string>();
        }

        var list = values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToList();

        return list.AsReadOnly();
    }

    private static IReadOnlyDictionary<string, string> CopyInjectedArguments(IEnumerable<KeyValuePair<string, string>>? values)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, value) in values ?? Array.Empty<KeyValuePair<string, string>>())
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Injected argument key cannot be empty.", nameof(values));
            }

            map[key.Trim()] = value ?? string.Empty;
        }

        return new ReadOnlyDictionary<string, string>(map);
    }
}
