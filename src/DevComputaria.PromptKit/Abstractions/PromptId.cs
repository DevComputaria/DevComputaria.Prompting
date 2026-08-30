namespace DevComputaria.PromptKit.Abstractions;

public readonly record struct PromptId
{
    public string Name { get; }

    public string Version { get; }

    public PromptId(string name, string version)
    {
        Name = ValidatePart(name, nameof(name));
        Version = ValidatePart(version, nameof(version));
    }

    public override string ToString() => $"{Name}@{Version}";

    private static string ValidatePart(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null, empty, or whitespace.", paramName);
        }

        return value.Trim();
    }
}
