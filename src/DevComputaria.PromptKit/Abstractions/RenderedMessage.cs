namespace DevComputaria.PromptKit.Abstractions;

public sealed record RenderedMessage
{
    public RenderedMessage(string role, string content, string? source = null)
    {
        Role = Validate(role, nameof(role));
        Content = Validate(content, nameof(content));
        Source = string.IsNullOrWhiteSpace(source) ? null : source.Trim();
    }

    public string Role { get; }

    public string Content { get; }

    public string? Source { get; }

    private static string Validate(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null, empty, or whitespace.", paramName);
        }

        return value.Trim();
    }
}
