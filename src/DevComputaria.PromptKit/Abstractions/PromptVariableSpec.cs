namespace DevComputaria.PromptKit.Abstractions;

public sealed record PromptVariableSpec
{
    public PromptVariableSpec(string name, bool isRequired, bool redactedInLogs = false, string? type = null)
    {
        Name = ValidateName(name);
        IsRequired = isRequired;
        RedactedInLogs = redactedInLogs;
        Type = string.IsNullOrWhiteSpace(type) ? null : type.Trim();
    }

    public string Name { get; }

    public bool IsRequired { get; }

    public bool RedactedInLogs { get; }

    public string? Type { get; }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Variable name cannot be null, empty, or whitespace.", nameof(name));
        }

        return name.Trim();
    }
}
