using System.Collections;
using System.Collections.ObjectModel;

namespace DevComputaria.PromptKit.Abstractions;

public sealed class PromptArgs : IReadOnlyDictionary<string, object?>
{
    public static readonly PromptArgs Empty = new();

    private readonly IReadOnlyDictionary<string, object?> _values;

    public PromptArgs()
        : this(new Dictionary<string, object?>())
    {
    }

    public PromptArgs(IEnumerable<KeyValuePair<string, object?>> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var copy = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var (key, value) in values)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Argument key cannot be null, empty, or whitespace.", nameof(values));
            }

            copy[key.Trim()] = value;
        }

        _values = new ReadOnlyDictionary<string, object?>(copy);
    }

    public object? this[string key] => _values[key];

    public IEnumerable<string> Keys => _values.Keys;

    public IEnumerable<object?> Values => _values.Values;

    public int Count => _values.Count;

    public bool ContainsKey(string key) => _values.ContainsKey(key);

    public bool TryGetValue(string key, out object? value) => _values.TryGetValue(key, out value);

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
