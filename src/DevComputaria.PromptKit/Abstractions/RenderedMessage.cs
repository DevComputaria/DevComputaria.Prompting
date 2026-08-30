namespace DevComputaria.PromptKit.Abstractions;

public sealed record RenderedMessage
{
    public RenderedMessage(string role, string content)
    {
        Role = Validate(role, nameof(role));
        Content = Validate(content, nameof(content));
    }

    public string Role { get; }

    public string Content { get; }

    private static string Validate(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null, empty, or whitespace.", paramName);
        }

        return value.Trim();
    }
}
