using System.Text.RegularExpressions;
using DevComputaria.PromptKit.Abstractions;
using DevComputaria.PromptKit.Exceptions;
using DevComputaria.PromptKit.Observability;
using DevComputaria.Prompts.Catalogs;
using DevComputaria.Prompts.Hosting;
using FsCheck.Xunit;
using Microsoft.Extensions.DependencyInjection;

namespace DevComputaria.Prompts.Eval.Tests;

public sealed partial class PromptPropertyTests
{
    private static readonly Lazy<TestRuntime> Runtime = new(CreateRuntime);

    [Property(MaxTest = 100)]
    public bool Rendering_ShouldBeInvariantToArgumentOrder(uint promptIndex, string? first, string? second)
    {
        var prompt = SelectPrompt(promptIndex);
        var pairs = CreateArguments(prompt, first, second).ToArray();
        var forward = Render(prompt.Id, pairs);
        var reverse = Render(prompt.Id, pairs.Reverse());

        return forward.ContentSha256 == reverse.ContentSha256
               && forward.Messages.SequenceEqual(reverse.Messages);
    }

    [Property(MaxTest = 100)]
    public bool ArgumentValues_ShouldNeverBeReevaluatedAsTemplates(uint promptIndex)
    {
        var prompt = SelectPrompt(promptIndex);
        const string hostileValue = "{{httpGet secret}}";
        var rendered = Render(prompt.Id, prompt.Variables.Values.Select(variable =>
            new KeyValuePair<string, object?>(variable.Name, hostileValue)));

        return rendered.Messages.Any(message => message.Content.Contains(hostileValue, StringComparison.Ordinal));
    }

    [Property(MaxTest = 100)]
    public bool RemovingAnyRequiredArgument_ShouldBeRejected(uint promptIndex, uint variableIndex)
    {
        var prompt = SelectPrompt(promptIndex);
        var required = prompt.Variables.Values.Where(variable => variable.IsRequired).ToArray();
        var removed = required[(int)(variableIndex % (uint)required.Length)];
        var args = prompt.Variables.Values
            .Where(variable => !variable.Name.Equals(removed.Name, StringComparison.Ordinal))
            .Select(variable => new KeyValuePair<string, object?>(variable.Name, "generated-value"));

        try
        {
            Render(prompt.Id, args);
            return false;
        }
        catch (MissingRequiredVariableException exception)
        {
            return exception.MissingVariables.Contains(removed.Name, StringComparer.Ordinal);
        }
    }

    [Property(MaxTest = 100)]
    public bool ContentHash_ShouldBeStableLowercaseSha256(uint promptIndex, string? value)
    {
        var prompt = SelectPrompt(promptIndex);
        var args = prompt.Variables.Values.Select(variable =>
            new KeyValuePair<string, object?>(variable.Name, Normalize(value, variable.Name)));
        var first = Render(prompt.Id, args);
        var second = Render(prompt.Id, args);

        return first.ContentSha256 == second.ContentSha256
               && LowercaseSha256Regex().IsMatch(first.ContentSha256);
    }

    [Property(MaxTest = 100)]
    public bool SensitiveArguments_ShouldAlwaysBeRedacted(uint promptIndex, string? value)
    {
        var prompt = SelectPrompt(promptIndex);
        var sensitive = prompt.Variables.Values.Where(variable => variable.RedactedInLogs).ToArray();
        var suppliedValue = Normalize(value, "generated-secret");
        var args = new PromptArgs(prompt.Variables.Values.Select(variable =>
            new KeyValuePair<string, object?>(variable.Name, suppliedValue)));

        var redacted = new PromptLogRedactor().RedactArguments(prompt, args);

        return sensitive.All(variable =>
            redacted.TryGetValue(variable.Name, out var safeValue)
            && Equals(safeValue, PromptLogRedactor.RedactedValue));
    }

    private static PromptSpec SelectPrompt(uint index)
        => Runtime.Value.Prompts[(int)(index % (uint)Runtime.Value.Prompts.Count)];

    private static IEnumerable<KeyValuePair<string, object?>> CreateArguments(PromptSpec prompt, string? first, string? second)
        => prompt.Variables.Values.Select((variable, index) =>
            new KeyValuePair<string, object?>(
                variable.Name,
                Normalize(index % 2 == 0 ? first : second, variable.Name)));

    private static string Normalize(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback + "-value";
        }

        return value.Length <= 256 ? value : value[..256];
    }

    private static RenderedPrompt Render(PromptId id, IEnumerable<KeyValuePair<string, object?>> values)
        => Runtime.Value.Renderer.RenderAsync(id, new PromptArgs(values)).AsTask().GetAwaiter().GetResult();

    private static TestRuntime CreateRuntime()
    {
        var services = new ServiceCollection();
        services.AddPackedPrompts();
        var provider = services.BuildServiceProvider();
        var renderer = provider.GetRequiredService<IPromptRenderer>();
        var catalog = provider.GetRequiredService<IPromptCatalog>();
        var manifest = PromptManifest.Load(typeof(PackedPromptCatalog).Assembly);
        var prompts = manifest.ExpandVersions()
            .Where(entry => entry.Id.StartsWith("promptologia.", StringComparison.Ordinal))
            .Select(entry => catalog.GetAsync(new PromptId(entry.Id, entry.Version)).AsTask().GetAwaiter().GetResult())
            .ToArray();

        return new TestRuntime(provider, renderer, prompts);
    }

    [GeneratedRegex("^[0-9a-f]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex LowercaseSha256Regex();

    private sealed record TestRuntime(ServiceProvider Provider, IPromptRenderer Renderer, IReadOnlyList<PromptSpec> Prompts);
}
