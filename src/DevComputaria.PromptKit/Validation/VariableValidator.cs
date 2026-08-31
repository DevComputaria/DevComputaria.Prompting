using DevComputaria.PromptKit.Abstractions;
using DevComputaria.PromptKit.Exceptions;

namespace DevComputaria.PromptKit.Validation;

public sealed class VariableValidator : IPromptSanitizer
{
    public ValueTask<PromptArgs> SanitizeAsync(PromptSpec prompt, PromptArgs args, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentNullException.ThrowIfNull(args);

        cancellationToken.ThrowIfCancellationRequested();

        var missingVariables = prompt.Variables
            .Values
            .Where(variable => variable.IsRequired)
            .Where(variable => !HasValue(args, variable.Name))
            .Select(variable => variable.Name)
            .ToArray();

        if (missingVariables.Length > 0)
        {
            throw new MissingRequiredVariableException(prompt.Id, missingVariables);
        }

        return ValueTask.FromResult(args);
    }

    private static bool HasValue(PromptArgs args, string variableName)
    {
        if (!args.TryGetValue(variableName, out var value) || value is null)
        {
            return false;
        }

        return value is not string text || !string.IsNullOrWhiteSpace(text);
    }
}