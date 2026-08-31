using System.Text.RegularExpressions;
using DevComputaria.PromptKit.Abstractions;
using DevComputaria.PromptKit.Exceptions;

namespace DevComputaria.PromptKit.Rendering;

public sealed partial class TemplateSandbox
{
    private static readonly HashSet<string> AllowedBlockHelpers = new(StringComparer.Ordinal)
    {
        "if",
        "unless",
        "else"
    };

    public void Validate(PromptId promptId, string template)
    {
        ArgumentNullException.ThrowIfNull(template);

        foreach (Match match in ExpressionRegex().Matches(template))
        {
            var expression = match.Groups[1].Value.Trim();
            if (string.IsNullOrWhiteSpace(expression) || expression.StartsWith('!'))
            {
                continue;
            }

            if (expression.StartsWith('/'))
            {
                continue;
            }

            if (expression.StartsWith('#'))
            {
                var blockName = ReadToken(expression[1..]);
                if (!AllowedBlockHelpers.Contains(blockName))
                {
                    throw new UnsafeTemplateHelperException(promptId, blockName);
                }

                continue;
            }

            if (expression.Equals("else", StringComparison.Ordinal))
            {
                continue;
            }

            var token = ReadToken(expression);
            if (!expression.Contains(' ', StringComparison.Ordinal) && !expression.Contains('\t', StringComparison.Ordinal))
            {
                continue;
            }

            throw new UnsafeTemplateHelperException(promptId, token);
        }
    }

    private static string ReadToken(string expression)
    {
        var token = expression
            .Split(new[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries)[0]
            .Trim();

        return token;
    }

    [GeneratedRegex("\\{\\{(.*?)\\}\\}", RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex ExpressionRegex();
}