namespace DevComputaria.PromptKit.Abstractions;

public sealed record SkillReference
{
    public SkillReference(string id, string version, string attach)
    {
        Id = Validate(id, nameof(id));
        Version = Validate(version, nameof(version));
        Attach = Validate(attach, nameof(attach)).ToLowerInvariant();

        if (Attach is not ("system" or "tool" or "both"))
        {
            throw new ArgumentException("Attach must be 'system', 'tool', or 'both'.", nameof(attach));
        }
    }

    public string Id { get; }

    public string Version { get; }

    public string Attach { get; }

    private static string Validate(string value, string paramName)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value cannot be null, empty, or whitespace.", paramName)
            : value.Trim();
}
