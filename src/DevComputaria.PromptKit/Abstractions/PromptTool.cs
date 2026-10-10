namespace DevComputaria.PromptKit.Abstractions;

public sealed record PromptTool
{
    public PromptTool(string name, string description, string inputSchemaJson)
    {
        Name = Validate(name, nameof(name));
        Description = Validate(description, nameof(description));
        InputSchemaJson = Validate(inputSchemaJson, nameof(inputSchemaJson));
    }

    public string Name { get; }

    public string Description { get; }

    public string InputSchemaJson { get; }

    private static string Validate(string value, string paramName)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value cannot be null, empty, or whitespace.", paramName)
            : value.Trim();
}
