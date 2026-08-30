using System.Collections.ObjectModel;

namespace DevComputaria.PromptKit.Abstractions;

public sealed record RenderedPrompt
{
    public RenderedPrompt(
        PromptId id,
        string contentSha256,
        IEnumerable<RenderedMessage> messages,
        string? packageVersion = null,
        IEnumerable<KeyValuePair<string, string?>>? hints = null)
    {
        Id = id;
        ContentSha256 = Validate(contentSha256, nameof(contentSha256));
        PackageVersion = string.IsNullOrWhiteSpace(packageVersion) ? null : packageVersion.Trim();
        Messages = CopyMessages(messages);
        Hints = CopyHints(hints);
    }

    public PromptId Id { get; }

    public string ContentSha256 { get; }

    public string? PackageVersion { get; }

    public IReadOnlyList<RenderedMessage> Messages { get; }

    public IReadOnlyDictionary<string, string?> Hints { get; }

    private static IReadOnlyList<RenderedMessage> CopyMessages(IEnumerable<RenderedMessage> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);
        var list = messages.ToList();
        return list.AsReadOnly();
    }

    private static IReadOnlyDictionary<string, string?> CopyHints(IEnumerable<KeyValuePair<string, string?>>? hints)
    {
        if (hints is null)
        {
            return new ReadOnlyDictionary<string, string?>(new Dictionary<string, string?>(StringComparer.Ordinal));
        }

        var map = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var (key, value) in hints)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Hint key cannot be null, empty, or whitespace.", nameof(hints));
            }

            map[key.Trim()] = value;
        }

        return new ReadOnlyDictionary<string, string?>(map);
    }

    private static string Validate(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null, empty, or whitespace.", paramName);
        }

        return value.Trim();
    }
}
