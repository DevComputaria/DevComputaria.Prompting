using DevComputaria.PromptKit.Abstractions;
using DevComputaria.PromptKit.Composition;
using DevComputaria.PromptKit.Hashing;
using DevComputaria.PromptKit.Rendering;
using DevComputaria.PromptKit.Validation;
using DevComputaria.Prompts.Catalogs;
using Xunit;

namespace DevComputaria.Prompts.Tests;

public sealed class ExtensionSetTests
{
    [Fact]
    public async Task V11Render_ShouldInjectSchemaAndAttachPinnedSkillWithoutCallerSchemaArgument()
    {
        var catalog = new PackedPromptCatalog(typeof(PackedPromptCatalog).Assembly);
        var renderer = new HandlebarsPromptRenderer(
            catalog,
            new PassthroughPromptComposer(),
            new VariableValidator(),
            new TemplateSandbox(),
            new PromptHasher());

        var rendered = await renderer.RenderAsync(
            global::DevComputaria.Prompts.Ids.PromptPins.ImageAnalysisAnalyzeDocument.V1_1_0,
            new PromptArgs(new Dictionary<string, object?>
            {
                ["country"] = "BR",
                ["document_type"] = "identity-card",
                ["ocr_text"] = "OCR-123"
            }));

        Assert.Contains(rendered.Messages, message =>
            message.Content.Contains("{\"document_type\":\"<string>\",\"country\":\"<string>\",\"confidence\":0}", StringComparison.Ordinal));
        Assert.Contains(rendered.Messages, message => message.Content.Contains("Preserve only information", StringComparison.Ordinal));
        Assert.Equal("record_document_confidence", Assert.Single(rendered.Tools).Name);
    }

    [Fact]
    public void FromContent_ShouldParticipateInContentHash()
    {
        const string yaml = """
                            id: sample.from
                            version: 1.1.0
                            parts:
                              - role: system
                                from: system.md
                            """;
        var loader = new YamlPromptLoader();
        var promptA = loader.Load(yaml, _ => "First external body");
        var promptB = loader.Load(yaml, _ => "Changed external body");
        var hasher = new PromptHasher();

        var hashA = hasher.ComputeHash(promptA, PromptArgs.Empty, promptA.Parts);
        var hashB = hasher.ComputeHash(promptB, PromptArgs.Empty, promptB.Parts);

        Assert.NotEqual(hashA, hashB);
    }

    [Fact]
    public async Task V11Render_ShouldRejectCallerManagedSchemaArgument()
    {
        var catalog = new PackedPromptCatalog(typeof(PackedPromptCatalog).Assembly);
        var renderer = new HandlebarsPromptRenderer(
            catalog,
            new PassthroughPromptComposer(),
            new VariableValidator(),
            new TemplateSandbox(),
            new PromptHasher());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await renderer.RenderAsync(
                global::DevComputaria.Prompts.Ids.PromptPins.ImageAnalysisAnalyzeDocument.V1_1_0,
                new PromptArgs(new Dictionary<string, object?>
                {
                    ["country"] = "BR",
                    ["document_type"] = "identity-card",
                    ["ocr_text"] = "OCR-123",
                    ["output_schema"] = "caller-controlled"
                })));

        Assert.Contains("managed by the prompt contract", exception.Message, StringComparison.Ordinal);
    }
}
