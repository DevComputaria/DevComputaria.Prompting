using DevComputaria.Prompts.Catalogs;
using Xunit;

namespace DevComputaria.Prompts.Tests;

public sealed class PromptManifestConsistencyValidatorTests
{
    private readonly PromptManifestConsistencyValidator _validator = new();

    [Fact]
    public void ValidateOrThrow_ShouldPassForMatchingManifestAndResources()
    {
        var manifest = PromptManifest.Load("""
                                         package: DevComputaria.Prompts
                                         schema: 1
                                         aliases:
                                           image-analysis.analyze-document: 1.0.0
                                         prompts:
                                           - id: image-analysis.analyze-document
                                             versions: [1.0.0]
                                             tags: [image-analysis]
                                         """);
        var resources = new[]
        {
            PromptResourceNames.Catalog,
            "prompts.image-analysis/analyze-document/1.0.0.yaml",
            "prompts._shared/json-only.yaml"
        };

        _validator.ValidateOrThrow(manifest, resources);
    }

    [Fact]
    public void ValidateOrThrow_ShouldFailForBrokenAlias()
    {
        var manifest = PromptManifest.Load("""
                                         package: DevComputaria.Prompts
                                         schema: 1
                                         aliases:
                                           image-analysis.analyze-document: 2.0.0
                                         prompts:
                                           - id: image-analysis.analyze-document
                                             versions: [1.0.0]
                                             tags: [image-analysis]
                                         """);
        var resources = new[]
        {
            PromptResourceNames.Catalog,
            "prompts.image-analysis/analyze-document/1.0.0.yaml"
        };

        var exception = Assert.Throws<PromptManifestConsistencyException>(() => _validator.ValidateOrThrow(manifest, resources));

        Assert.Contains(exception.Issues, issue => issue.Contains("Alias 'image-analysis.analyze-document' points to unpublished version '2.0.0'", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateOrThrow_ShouldFailWhenManifestEntryHasNoResource()
    {
        var manifest = PromptManifest.Load("""
                                         package: DevComputaria.Prompts
                                         schema: 1
                                         aliases: {}
                                         prompts:
                                           - id: image-analysis.analyze-document
                                             versions: [1.0.0]
                                             tags: [image-analysis]
                                         """);
        var resources = new[]
        {
            PromptResourceNames.Catalog
        };

        var exception = Assert.Throws<PromptManifestConsistencyException>(() => _validator.ValidateOrThrow(manifest, resources));

        Assert.Contains(exception.Issues, issue => issue.Contains("points to missing resource 'prompts.image-analysis/analyze-document/1.0.0.yaml'", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateOrThrow_ShouldFailForOrphanEmbeddedResource()
    {
        var manifest = PromptManifest.Load("""
                                         package: DevComputaria.Prompts
                                         schema: 1
                                         aliases: {}
                                         prompts:
                                           - id: image-analysis.analyze-document
                                             versions: [1.0.0]
                                             tags: [image-analysis]
                                         """);
        var resources = new[]
        {
            PromptResourceNames.Catalog,
            "prompts.image-analysis/analyze-document/1.0.0.yaml",
            "prompts.image-analysis/extract-qr/1.0.0.yaml"
        };

        var exception = Assert.Throws<PromptManifestConsistencyException>(() => _validator.ValidateOrThrow(manifest, resources));

        Assert.Contains(exception.Issues, issue => issue.Contains("embedded but not declared in the manifest", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateOrThrow_ShouldValidateCurrentAssemblyResources()
    {
        var assembly = typeof(PackedPromptCatalog).Assembly;
        var manifest = PromptManifest.Load(assembly);

        _validator.ValidateOrThrow(assembly, manifest);
    }
}