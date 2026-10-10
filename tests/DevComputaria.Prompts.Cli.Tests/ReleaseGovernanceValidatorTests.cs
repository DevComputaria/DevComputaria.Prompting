using Xunit;

namespace DevComputaria.Prompts.Cli.Tests;

public sealed class ReleaseGovernanceValidatorTests
{
    private readonly ReleaseGovernanceValidator _validator = new();

    [Fact]
    public void Validate_NewMinorPromptVersion_ShouldRequireMinorImpact()
    {
        var result = _validator.Validate(
            [new RepositoryChange("A", "prompts/image-analysis/analyze-document/1.1.0.yaml")],
            ["prompts/image-analysis/analyze-document/1.0.0.yaml"],
            SemVersionImpact.Minor);

        Assert.True(result.IsValid);
        Assert.Equal(SemVersionImpact.Minor, result.RequiredImpact);
    }

    [Fact]
    public void Validate_ModifiedPublishedArtifact_ShouldFailRegardlessOfDeclaredImpact()
    {
        const string path = "skills/image-analysis/document-extractor/1.0.0.yaml";

        var result = _validator.Validate(
            [new RepositoryChange("M", path)],
            [path],
            SemVersionImpact.Major);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Contains("cannot be modified in place", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_RemovedPinnedVersion_ShouldRequireMajorImpact()
    {
        const string path = "prompts/image-analysis/analyze-document/1.0.0.yaml";

        var result = _validator.Validate(
            [new RepositoryChange("D", path)],
            [path],
            SemVersionImpact.Minor);

        Assert.False(result.IsValid);
        Assert.Equal(SemVersionImpact.Major, result.RequiredImpact);
        Assert.Contains(result.Issues, issue => issue.Contains("lower than required MAJOR", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_NewOutputSchemaRevision_ShouldRequireMajorImpact()
    {
        var result = _validator.Validate(
            [new RepositoryChange("A", "schemas/output/image-analysis-document-v2.json")],
            ["schemas/output/image-analysis-document-v1.json"],
            SemVersionImpact.Major);

        Assert.True(result.IsValid);
        Assert.Equal(SemVersionImpact.Major, result.RequiredImpact);
    }

    [Theory]
    [InlineData("1.2.3", "1.2.4", SemVersionImpact.Patch)]
    [InlineData("1.2.3", "1.3.0", SemVersionImpact.Minor)]
    [InlineData("1.2.3", "2.0.0", SemVersionImpact.Major)]
    public void Validate_PackageVersionTransition_ShouldMatchDeclaredImpact(
        string previous,
        string next,
        SemVersionImpact impact)
    {
        var result = _validator.Validate(
            [new RepositoryChange("M", "README.md")],
            Array.Empty<string>(),
            impact,
            previous,
            next);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_MismatchedPackageVersionTransition_ShouldFail()
    {
        var result = _validator.Validate(
            [new RepositoryChange("M", "src/DevComputaria.PromptKit/Observability/PromptLogRedactor.cs")],
            Array.Empty<string>(),
            SemVersionImpact.Minor,
            "1.2.3",
            "1.2.4");

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Contains("is PATCH, not declared MINOR", StringComparison.Ordinal));
    }
}
