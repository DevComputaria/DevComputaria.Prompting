using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using DevComputaria.PromptKit.Abstractions;

namespace DevComputaria.PromptKit.Rendering;

public sealed partial class HandlebarsPromptRenderer : IPromptRenderer
{
    private readonly IPromptCatalog _catalog;
    private readonly IPromptComposer _composer;
    private readonly IPromptSanitizer _sanitizer;
    private readonly TemplateSandbox _sandbox;
    private readonly string? _packageVersion;

    public HandlebarsPromptRenderer(
        IPromptCatalog catalog,
        IPromptComposer composer,
        IPromptSanitizer sanitizer,
        TemplateSandbox sandbox,
        string? packageVersion = null)
    {
        _catalog = catalog;
        _composer = composer;
        _sanitizer = sanitizer;
        _sandbox = sandbox;
        _packageVersion = string.IsNullOrWhiteSpace(packageVersion) ? null : packageVersion.Trim();
    }

    public async ValueTask<RenderedPrompt> RenderAsync(PromptId id, PromptArgs args, CancellationToken cancellationToken = default)
    {
        var prompt = await _catalog.GetAsync(id, cancellationToken);
        var composed = await _composer.ComposeAsync(prompt, cancellationToken);
        var sanitized = await _sanitizer.SanitizeAsync(composed, args, cancellationToken);

        var renderedMessages = composed.Parts
            .Select(part => RenderPart(composed.Id, part, sanitized))
            .ToArray();

        return new RenderedPrompt(
            composed.Id,
            ComputeHash(renderedMessages),
            renderedMessages,
            packageVersion: _packageVersion,
            hints: new Dictionary<string, string?>
            {
                ["renderer"] = "handlebars-sandbox"
            });
    }

    private RenderedMessage RenderPart(PromptId promptId, RenderedMessage part, PromptArgs args)
    {
        _sandbox.Validate(promptId, part.Content);

        var rendered = RenderTemplate(part.Content, args);
        return new RenderedMessage(part.Role, rendered);
    }

    private static string RenderTemplate(string template, PromptArgs args)
    {
        var rendered = template;

        while (true)
        {
            var updated = IfBlockRegex().Replace(rendered, match => RenderIfBlock(match, args));
            updated = UnlessBlockRegex().Replace(updated, match => RenderUnlessBlock(match, args));

            if (updated.Equals(rendered, StringComparison.Ordinal))
            {
                break;
            }

            rendered = updated;
        }

        rendered = TripleVariableRegex().Replace(rendered, match => ResolveVariable(match.Groups[1].Value, args));
        rendered = VariableRegex().Replace(rendered, match => ResolveVariable(match.Groups[1].Value, args));

        return rendered;
    }

    private static string RenderIfBlock(Match match, PromptArgs args)
    {
        var variableName = match.Groups[1].Value;
        var whenTrue = match.Groups[2].Value;
        var whenFalse = match.Groups[4].Success ? match.Groups[4].Value : string.Empty;

        return HasTruthyValue(args, variableName) ? whenTrue : whenFalse;
    }

    private static string RenderUnlessBlock(Match match, PromptArgs args)
    {
        var variableName = match.Groups[1].Value;
        var whenFalse = match.Groups[2].Value;
        var whenTrue = match.Groups[4].Success ? match.Groups[4].Value : string.Empty;

        return HasTruthyValue(args, variableName) ? whenTrue : whenFalse;
    }

    private static bool HasTruthyValue(PromptArgs args, string variableName)
    {
        if (!args.TryGetValue(variableName.Trim(), out var value) || value is null)
        {
            return false;
        }

        return value switch
        {
            bool boolean => boolean,
            string text => !string.IsNullOrWhiteSpace(text),
            _ => true
        };
    }

    private static string ResolveVariable(string variableName, PromptArgs args)
    {
        return args.TryGetValue(variableName.Trim(), out var value) && value is not null
            ? value.ToString() ?? string.Empty
            : string.Empty;
    }

    private static string ComputeHash(IEnumerable<RenderedMessage> messages)
    {
        var canonical = string.Join(
            "\n---\n",
            messages.Select(message => $"role:{message.Role}\ncontent:{message.Content}"));

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    [GeneratedRegex("\\{\\{#if\\s+([^}]+)\\}\\}(.*?)(\\{\\{else\\}\\}(.*?))?\\{\\{/if\\}\\}", RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex IfBlockRegex();

    [GeneratedRegex("\\{\\{#unless\\s+([^}]+)\\}\\}(.*?)(\\{\\{else\\}\\}(.*?))?\\{\\{/unless\\}\\}", RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex UnlessBlockRegex();

    [GeneratedRegex("\\{\\{\\{\\s*([^}]+?)\\s*\\}\\}\\}", RegexOptions.CultureInvariant)]
    private static partial Regex TripleVariableRegex();

    [GeneratedRegex("\\{\\{\\s*([a-zA-Z0-9_.-]+)\\s*\\}\\}", RegexOptions.CultureInvariant)]
    private static partial Regex VariableRegex();
}