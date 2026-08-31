using Xunit;

namespace DevComputaria.Prompts.Contract.Tests;

public sealed class SchemaValidationTests
{
    private readonly SchemaSubsetValidator _validator = new();

    [Fact]
    public void PromptYaml_FromRepository_ShouldMatchPromptSchema()
    {
        var schema = File.ReadAllText(ContractTestPaths.ResolveFromRoot("schemas", "prompt.schema.json"));
        var yaml = File.ReadAllText(ContractTestPaths.ResolveFromRoot("prompts", "image-analysis", "analyze-document", "1.0.0.yaml"));

        var errors = _validator.ValidateYaml(schema, yaml);

        Assert.Empty(errors);
    }

    [Fact]
    public void CatalogYaml_FromRepository_ShouldMatchCatalogSchema()
    {
        var schema = File.ReadAllText(ContractTestPaths.ResolveFromRoot("schemas", "catalog.schema.json"));
        var yaml = File.ReadAllText(ContractTestPaths.ResolveFromRoot("prompts", "catalog.yaml"));

        var errors = _validator.ValidateYaml(schema, yaml);

        Assert.Empty(errors);
    }

    [Fact]
    public void InvalidPromptYaml_ShouldFailAgainstPromptSchema()
    {
        var schema = File.ReadAllText(ContractTestPaths.ResolveFromRoot("schemas", "prompt.schema.json"));
        var invalidYaml = """
                          id: image-analysis.analyze-document
                          version: 1.0
                          parts:
                            - role: assistant
                              template: bad
                          """;

        var errors = _validator.ValidateYaml(schema, invalidYaml);

        Assert.NotEmpty(errors);
        Assert.Contains(errors, error => error.Contains("pattern", StringComparison.Ordinal) || error.Contains("not one of", StringComparison.Ordinal));
    }

    [Fact]
    public void InvalidCatalogYaml_ShouldFailAgainstCatalogSchema()
    {
        var schema = File.ReadAllText(ContractTestPaths.ResolveFromRoot("schemas", "catalog.schema.json"));
        var invalidYaml = """
                          package: DevComputaria.Prompts
                          schema: 1
                          prompts:
                            - id: image-analysis.analyze-document
                              versions: [latest]
                          """;

        var errors = _validator.ValidateYaml(schema, invalidYaml);

        Assert.NotEmpty(errors);
        Assert.Contains(errors, error => error.Contains("pattern", StringComparison.Ordinal));
    }
}