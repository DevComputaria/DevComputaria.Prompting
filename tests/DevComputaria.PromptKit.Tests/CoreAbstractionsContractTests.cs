using DevComputaria.PromptKit.Abstractions;
using DevComputaria.PromptKit.Catalogs;
using DevComputaria.PromptKit.Exceptions;
using DevComputaria.PromptKit.Validation;
using Xunit;

namespace DevComputaria.PromptKit.Tests;

public sealed class CoreAbstractionsContractTests
{
    [Fact]
    public void PromptId_ShouldNormalizeAndFormat()
    {
        var id = new PromptId(" image-analysis.analyze-document ", " 1.0.0 ");

        Assert.Equal("image-analysis.analyze-document", id.Name);
        Assert.Equal("1.0.0", id.Version);
        Assert.Equal("image-analysis.analyze-document@1.0.0", id.ToString());
    }

    [Fact]
    public void PromptArgs_ShouldBeImmutableCopy()
    {
        var source = new Dictionary<string, object?> { ["country"] = "BR" };
        var args = new PromptArgs(source);

        source["country"] = "US";

        Assert.True(args.ContainsKey("country"));
        Assert.Equal("BR", args["country"]);
    }

    [Fact]
    public void PromptSpec_ShouldCreateImmutableCollections()
    {
        var parts = new List<RenderedMessage>
        {
            new("system", "You extract image analysis."),
            new("user", "Country: {{country}}")
        };

        var variables = new List<PromptVariableSpec>
        {
            new("country", isRequired: true, redactedInLogs: false, type: "string")
        };

        var includes = new List<string> { "_shared/json-only" };

        var spec = new PromptSpec(
            new PromptId("image-analysis.analyze-document", "1.0.0"),
            parts,
            variables,
            includes,
            outputSchemaRef: "schemas/output/image-analysis-document-v1.json");

        parts.Add(new RenderedMessage("assistant", "not allowed"));
        variables.Add(new PromptVariableSpec("ocr_text", isRequired: true, redactedInLogs: true));
        includes.Add("_shared/no-invention");

        Assert.Equal(2, spec.Parts.Count);
        Assert.Single(spec.Variables);
        Assert.Single(spec.Includes);
    }

    [Fact]
    public void RenderedPrompt_ShouldCaptureMetadataAndCopyCollections()
    {
        var messages = new List<RenderedMessage>
        {
            new("system", "Answer only JSON."),
            new("user", "OCR: ...")
        };

        var hints = new Dictionary<string, string?>
        {
            ["model"] = "gpt-4o-mini",
            ["temperature"] = "0"
        };

        var rendered = new RenderedPrompt(
            new PromptId("image-analysis.analyze-document", "1.0.0"),
            "abc123",
            messages,
            packageVersion: "1.0.0",
            hints: hints);

        messages.Clear();
        hints["temperature"] = "1";

        Assert.Equal("abc123", rendered.ContentSha256);
        Assert.Equal("1.0.0", rendered.PackageVersion);
        Assert.Equal(2, rendered.Messages.Count);
        Assert.Equal("0", rendered.Hints["temperature"]);
    }

    [Fact]
    public async Task Interfaces_ShouldBeTestableWithoutProviderDependencies()
    {
        var promptId = new PromptId("image-analysis.analyze-document", "1.0.0");
        var prompt = new PromptSpec(
            promptId,
            new[]
            {
                new RenderedMessage("system", "Answer only JSON."),
                new RenderedMessage("user", "Country: {{country}}")
            },
            new[] { new PromptVariableSpec("country", isRequired: true, type: "string") });

        IPromptCatalog catalog = new InMemoryPromptCatalog(new[] { prompt });
        IPromptComposer composer = new PassthroughComposer();
        IPromptSanitizer sanitizer = new VariableValidator();
        IPromptRenderer renderer = new FakeRenderer(catalog, composer, sanitizer);

        var rendered = await renderer.RenderAsync(promptId, new PromptArgs(new Dictionary<string, object?>
        {
            ["country"] = "BR"
        }));

        Assert.Equal(promptId, rendered.Id);
        Assert.Equal(2, rendered.Messages.Count);
        Assert.Equal("dev-sha256", rendered.ContentSha256);
    }

    [Fact]
    public async Task VariableValidator_ShouldThrowSpecificExceptionWhenRequiredVariableIsMissing()
    {
        var prompt = new PromptSpec(
            new PromptId("image-analysis.analyze-document", "1.0.0"),
            new[] { new RenderedMessage("system", "Answer only JSON.") },
            new[]
            {
                new PromptVariableSpec("country", isRequired: true, type: "string"),
                new PromptVariableSpec("ocr_text", isRequired: true, redactedInLogs: true, type: "string")
            });

        IPromptSanitizer validator = new VariableValidator();

        var exception = await Assert.ThrowsAsync<MissingRequiredVariableException>(async () =>
            await validator.SanitizeAsync(prompt, new PromptArgs(new Dictionary<string, object?>
            {
                ["country"] = "BR"
            })));

        Assert.Equal(prompt.Id, exception.PromptId);
        Assert.Equal(new[] { "ocr_text" }, exception.MissingVariables);
        Assert.Contains("ocr_text", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task VariableValidator_ShouldTreatNullOrWhitespaceRequiredValuesAsMissing()
    {
        var prompt = new PromptSpec(
            new PromptId("image-analysis.analyze-document", "1.0.0"),
            new[] { new RenderedMessage("system", "Answer only JSON.") },
            new[]
            {
                new PromptVariableSpec("country", isRequired: true, type: "string"),
                new PromptVariableSpec("document_type", isRequired: true, type: "string")
            });

        IPromptSanitizer validator = new VariableValidator();

        var exception = await Assert.ThrowsAsync<MissingRequiredVariableException>(async () =>
            await validator.SanitizeAsync(prompt, new PromptArgs(new Dictionary<string, object?>
            {
                ["country"] = "   ",
                ["document_type"] = null
            })));

        Assert.Equal(new[] { "country", "document_type" }, exception.MissingVariables);
    }

    [Fact]
    public async Task VariableValidator_ShouldAllowOptionalVariablesToBeMissing()
    {
        var prompt = new PromptSpec(
            new PromptId("image-analysis.analyze-document", "1.0.0"),
            new[] { new RenderedMessage("system", "Answer only JSON.") },
            new[]
            {
                new PromptVariableSpec("country", isRequired: true, type: "string"),
                new PromptVariableSpec("notes", isRequired: false, type: "string")
            });

        IPromptSanitizer validator = new VariableValidator();

        var sanitized = await validator.SanitizeAsync(prompt, new PromptArgs(new Dictionary<string, object?>
        {
            ["country"] = "BR"
        }));

        Assert.Same(sanitized, sanitized);
        Assert.Equal("BR", sanitized["country"]);
        Assert.False(sanitized.ContainsKey("notes"));
    }

    [Fact]
    public async Task InMemoryPromptCatalog_ShouldResolveExistingPromptByIdAndVersion()
    {
        var prompt = new PromptSpec(
            new PromptId("image-analysis.analyze-document", "1.0.0"),
            new[] { new RenderedMessage("system", "Answer only JSON.") });

        var catalog = new InMemoryPromptCatalog(new[] { prompt });

        var resolved = await catalog.GetAsync(new PromptId("image-analysis.analyze-document", "1.0.0"));

        Assert.Same(prompt, resolved);
    }

    [Fact]
    public async Task InMemoryPromptCatalog_ShouldThrowSpecificExceptionWhenPromptNameDoesNotExist()
    {
        var prompt = new PromptSpec(
            new PromptId("image-analysis.analyze-document", "1.0.0"),
            new[] { new RenderedMessage("system", "Answer only JSON.") });

        var catalog = new InMemoryPromptCatalog(new[] { prompt });

        var exception = await Assert.ThrowsAsync<PromptNotFoundException>(async () =>
            await catalog.GetAsync(new PromptId("image-analysis.classify-intent", "1.0.0")));

        Assert.Contains("image-analysis.classify-intent@1.0.0", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InMemoryPromptCatalog_ShouldThrowVersionMismatchWhenPromptVersionDoesNotExist()
    {
        var prompts = new[]
        {
            new PromptSpec(
                new PromptId("image-analysis.analyze-document", "1.0.0"),
                new[] { new RenderedMessage("system", "v1") }),
            new PromptSpec(
                new PromptId("image-analysis.analyze-document", "1.1.0"),
                new[] { new RenderedMessage("system", "v1.1") })
        };

        var catalog = new InMemoryPromptCatalog(prompts);

        var exception = await Assert.ThrowsAsync<PromptVersionMismatchException>(async () =>
            await catalog.GetAsync(new PromptId("image-analysis.analyze-document", "2.0.0")));

        Assert.Contains("image-analysis.analyze-document@2.0.0", exception.Message, StringComparison.Ordinal);
        Assert.Equal(new[] { "1.0.0", "1.1.0" }, exception.AvailableVersions);
    }

    private sealed class PassthroughComposer : IPromptComposer
    {
        public ValueTask<PromptSpec> ComposeAsync(PromptSpec prompt, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(prompt);
        }
    }

    private sealed class FakeRenderer : IPromptRenderer
    {
        private readonly IPromptCatalog _catalog;
        private readonly IPromptComposer _composer;
        private readonly IPromptSanitizer _sanitizer;

        public FakeRenderer(IPromptCatalog catalog, IPromptComposer composer, IPromptSanitizer sanitizer)
        {
            _catalog = catalog;
            _composer = composer;
            _sanitizer = sanitizer;
        }

        public async ValueTask<RenderedPrompt> RenderAsync(PromptId id, PromptArgs args, CancellationToken cancellationToken = default)
        {
            var spec = await _catalog.GetAsync(id, cancellationToken);

            _ = await _composer.ComposeAsync(spec, cancellationToken);
            _ = await _sanitizer.SanitizeAsync(spec, args, cancellationToken);

            return new RenderedPrompt(
                id,
                contentSha256: "dev-sha256",
                messages: spec.Parts,
                packageVersion: "test");
        }
    }
}
