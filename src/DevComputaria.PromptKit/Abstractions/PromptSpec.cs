using System.Collections.ObjectModel;

namespace DevComputaria.PromptKit.Abstractions;

public sealed record PromptSpec
{
    public PromptSpec(
        PromptId id,
        IEnumerable<RenderedMessage> parts,
        IEnumerable<PromptVariableSpec>? variables = null,
        IEnumerable<string>? includes = null,
        string? outputSchemaRef = null)
    {
        Id = id;
        Parts = Copy(parts);
        Variables = CopyVariables(variables);
        Includes = CopyStrings(includes);
        OutputSchemaRef = string.IsNullOrWhiteSpace(outputSchemaRef) ? null : outputSchemaRef.Trim();
    }

    public PromptId Id { get; }

    public IReadOnlyList<RenderedMessage> Parts { get; }

    public IReadOnlyDictionary<string, PromptVariableSpec> Variables { get; }

    public IReadOnlyList<string> Includes { get; }

    public string? OutputSchemaRef { get; }

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
}
