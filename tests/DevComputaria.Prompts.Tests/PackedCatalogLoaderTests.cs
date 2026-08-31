using DevComputaria.PromptKit.Abstractions;
using DevComputaria.Prompts.Catalogs;
using Xunit;

namespace DevComputaria.Prompts.Tests;

public sealed class PackedCatalogLoaderTests
{
    [Fact]
    public void YamlPromptLoader_ShouldHydratePromptSpecFromValidYaml()
    {
        var yaml = """
                   id: image-analysis.analyze-document
                   version: 1.0.0
                   includes: [_shared/json-only]
                   variables:
                     country: { type: string, required: true }
                     ocr_text: { type: string, required: true, redacted_in_logs: true }
                   output:
                     kind: json
                     schema_ref: schemas/output/image-analysis-document-v1.json
                   parts:
                     - role: system
                       template: |
                         Answer only JSON.
                     - role: user
                       template: |
                         Country: {{country}}
                   """;

        var loader = new YamlPromptLoader();

        var prompt = loader.Load(yaml);

        Assert.Equal(new PromptId("image-analysis.analyze-document", "1.0.0"), prompt.Id);
        Assert.Equal("schemas/output/image-analysis-document-v1.json", prompt.OutputSchemaRef);
        Assert.Single(prompt.Includes);
        Assert.Equal(2, prompt.Variables.Count);
        Assert.True(prompt.Variables["ocr_text"].RedactedInLogs);
        Assert.Equal(2, prompt.Parts.Count);
    }

    [Fact]
    public async Task PackedPromptCatalog_ShouldResolveLookupByIdAndVersion()
    {
        var catalog = new PackedPromptCatalog(typeof(PackedPromptCatalog).Assembly);

        var prompt = await catalog.GetAsync(new PromptId("image-analysis.analyze-document", "1.0.0"));

        Assert.Equal("image-analysis.analyze-document", prompt.Id.Name);
        Assert.Equal("1.0.0", prompt.Id.Version);
        Assert.Equal("schemas/output/image-analysis-document-v1.json", prompt.OutputSchemaRef);
        Assert.Contains(prompt.Parts, part => part.Role == "system");
        Assert.Contains(prompt.Parts, part => part.Role == "user");
    }

    [Fact]
    public void PromptResourceNames_ShouldGenerateStableLogicalNames()
    {
        var resourceName = PromptResourceNames.PromptFile("image-analysis.analyze-document", "1.0.0");

        Assert.Equal("prompts.image-analysis/analyze-document/1.0.0.yaml", resourceName);
        Assert.True(PromptResourceNames.IsPromptFile(resourceName));
        Assert.False(PromptResourceNames.IsPromptFile(PromptResourceNames.Catalog));
        Assert.False(PromptResourceNames.IsPromptFile("prompts._shared/json-only.yaml"));
    }

    [Fact]
    public void PromptManifest_ShouldExpandVersionsIntoStableResourceMappings()
    {
        var manifestYaml = """
                           package: DevComputaria.Prompts
                           schema: 1
                           aliases:
                             image-analysis.analyze-document: 1.0.0
                           prompts:
                             - id: image-analysis.analyze-document
                               versions: [1.0.0, 1.1.0]
                               tags: [image-analysis]
                           """;

        var manifest = PromptManifest.Load(manifestYaml);
        var entries = manifest.ExpandVersions().ToArray();

        Assert.Equal("DevComputaria.Prompts", manifest.Package);
        Assert.Equal(1, manifest.Schema);
        Assert.Equal(2, entries.Length);
        Assert.Equal("prompts.image-analysis/analyze-document/1.0.0.yaml", entries[0].ResourceName);
        Assert.Equal("prompts.image-analysis/analyze-document/1.1.0.yaml", entries[1].ResourceName);
    }
}