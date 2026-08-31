using DevComputaria.PromptKit.Abstractions;
using DevComputaria.PromptKit.Catalogs;
using DevComputaria.PromptKit.Exceptions;
using DevComputaria.PromptKit.Rendering;
using DevComputaria.PromptKit.Validation;
using Xunit;

namespace DevComputaria.PromptKit.Tests;

public sealed class HandlebarsPromptRendererTests
{
    [Fact]
    public async Task RenderAsync_ShouldSupportInterpolationAndConditionals()
    {
        var promptId = new PromptId("image-analysis.analyze-document", "1.0.0");
        var prompt = new PromptSpec(
            promptId,
            new[]
            {
                new RenderedMessage("system", "Answer only JSON. {{#if country}}Country={{country}}{{/if}}"),
                new RenderedMessage("user", "Type={{document_type}}{{#unless notes}}\nNo notes{{/unless}}")
            },
            new[]
            {
                new PromptVariableSpec("country", isRequired: true, type: "string"),
                new PromptVariableSpec("document_type", isRequired: true, type: "string"),
                new PromptVariableSpec("notes", isRequired: false, type: "string")
            });

        var renderer = CreateRenderer(prompt);

        var rendered = await renderer.RenderAsync(promptId, new PromptArgs(new Dictionary<string, object?>
        {
            ["country"] = "BR",
            ["document_type"] = "identity-card"
        }));

        Assert.Equal(promptId, rendered.Id);
        Assert.Equal(2, rendered.Messages.Count);
        Assert.Equal("Answer only JSON. Country=BR", rendered.Messages[0].Content);
        Assert.Equal("Type=identity-card\nNo notes", rendered.Messages[1].Content);
        Assert.Equal("handlebars-sandbox", rendered.Hints["renderer"]);
        Assert.False(string.IsNullOrWhiteSpace(rendered.ContentSha256));
    }

    [Fact]
    public async Task RenderAsync_ShouldBlockUnsafeHelpers()
    {
        var promptId = new PromptId("image-analysis.analyze-document", "1.0.0");
        var prompt = new PromptSpec(
            promptId,
            new[]
            {
                new RenderedMessage("system", "{{httpGet endpoint}}")
            },
            new[]
            {
                new PromptVariableSpec("endpoint", isRequired: true, type: "string")
            });

        var renderer = CreateRenderer(prompt);

        var exception = await Assert.ThrowsAsync<UnsafeTemplateHelperException>(async () =>
            await renderer.RenderAsync(promptId, new PromptArgs(new Dictionary<string, object?>
            {
                ["endpoint"] = "https://sensitive.example/api"
            })));

        Assert.Equal(promptId, exception.PromptId);
        Assert.Equal("httpGet", exception.HelperName);
    }

    [Fact]
    public async Task RenderAsync_ShouldNotLeakSensitiveArgumentValuesWhenHelperIsBlocked()
    {
        var promptId = new PromptId("image-analysis.analyze-document", "1.0.0");
        var secretPayload = "RG-123456-SECRET";
        var prompt = new PromptSpec(
            promptId,
            new[]
            {
                new RenderedMessage("system", "{{readFile ocr_text}}")
            },
            new[]
            {
                new PromptVariableSpec("ocr_text", isRequired: true, redactedInLogs: true, type: "string")
            });

        var renderer = CreateRenderer(prompt);

        var exception = await Assert.ThrowsAsync<UnsafeTemplateHelperException>(async () =>
            await renderer.RenderAsync(promptId, new PromptArgs(new Dictionary<string, object?>
            {
                ["ocr_text"] = secretPayload
            })));

        Assert.DoesNotContain(secretPayload, exception.Message, StringComparison.Ordinal);
        Assert.Contains("readFile", exception.Message, StringComparison.Ordinal);
    }

    private static HandlebarsPromptRenderer CreateRenderer(PromptSpec prompt)
    {
        return new HandlebarsPromptRenderer(
            new InMemoryPromptCatalog(new[] { prompt }),
            new PassthroughComposer(),
            new VariableValidator(),
            new TemplateSandbox(),
            packageVersion: "test");
    }

    private sealed class PassthroughComposer : IPromptComposer
    {
        public ValueTask<PromptSpec> ComposeAsync(PromptSpec prompt, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(prompt);
        }
    }
}