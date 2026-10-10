using System.Collections.ObjectModel;
using DevComputaria.PromptKit.Abstractions;

namespace DevComputaria.PromptKit.Observability;

public sealed class PromptLogRedactor
{
    public const string RedactedValue = "[REDACTED]";

    public IReadOnlyDictionary<string, object?> RedactArguments(PromptSpec prompt, PromptArgs args)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentNullException.ThrowIfNull(args);

        var safeValues = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var variable in prompt.Variables.Values.OrderBy(variable => variable.Name, StringComparer.Ordinal))
        {
            if (!args.TryGetValue(variable.Name, out var value))
            {
                continue;
            }

            safeValues[variable.Name] = variable.RedactedInLogs ? RedactedValue : value;
        }

        return new ReadOnlyDictionary<string, object?>(safeValues);
    }
}
