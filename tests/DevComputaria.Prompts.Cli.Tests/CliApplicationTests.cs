using System.Text.Json;
using Xunit;

namespace DevComputaria.Prompts.Cli.Tests;

public sealed class CliApplicationTests
{
    [Fact]
    public async Task Validate_CurrentRepository_ReturnsZero()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var application = new CliApplication(output, error);

        var exitCode = await application.RunAsync(["validate", "--root", RepositoryRoot]);

        Assert.Equal(0, exitCode);
        Assert.Contains("validation succeeded", output.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(string.Empty, error.ToString());
    }

    [Fact]
    public async Task Validate_MissingInclude_ReturnsOneWithDiagnostic()
    {
        var fixture = Directory.CreateTempSubdirectory("prompts-cli-");
        try
        {
            CreateInvalidRepository(fixture.FullName);
            using var output = new StringWriter();
            using var error = new StringWriter();
            var application = new CliApplication(output, error);

            var exitCode = await application.RunAsync(["validate", "--root", fixture.FullName]);

            Assert.Equal(1, exitCode);
            Assert.Contains("include '_shared/missing' was not found", error.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            fixture.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task Render_BaseFixture_ReturnsMessagesAndStableHash()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var application = new CliApplication(output, error);

        var exitCode = await application.RunAsync([
            "render",
            "image-analysis.analyze-document@1.0.0",
            "--arg", "country=BR",
            "--arg", "document_type=identity-card",
            "--arg", "ocr_text=OCR-123"
        ]);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, error.ToString());

        using var result = JsonDocument.Parse(output.ToString());
        Assert.Equal("39a5c0b4ef96fac1b6bd2862376eddd89d4c964d88e90894b6e27012aeba23d2",
            result.RootElement.GetProperty("contentSha256").GetString());
        Assert.Equal(2, result.RootElement.GetProperty("messages").GetArrayLength());
        Assert.Contains("OCR-123", result.RootElement.GetProperty("messages")[1].GetProperty("content").GetString());
    }

    [Fact]
    public async Task Render_MissingRequiredArgument_ReturnsOneWithDiagnostic()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var application = new CliApplication(output, error);

        var exitCode = await application.RunAsync([
            "render",
            "image-analysis.analyze-document@1.0.0",
            "--arg", "country=BR"
        ]);

        Assert.Equal(1, exitCode);
        Assert.Contains("document_type", error.ToString(), StringComparison.Ordinal);
        Assert.Contains("ocr_text", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReleaseCheck_Help_ShouldDescribeGovernanceContract()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var application = new CliApplication(output, error);

        var exitCode = await application.RunAsync(["release-check", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("--impact <patch|minor|major>", output.ToString(), StringComparison.Ordinal);
        Assert.Equal(string.Empty, error.ToString());
    }

    private static string RepositoryRoot { get; } = FindRepositoryRoot();

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "DevComputaria.Prompting.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root could not be found.");
    }

    private static void CreateInvalidRepository(string root)
    {
        Directory.CreateDirectory(Path.Combine(root, "schemas"));
        Directory.CreateDirectory(Path.Combine(root, "prompts", "example", "demo"));
        File.Copy(Path.Combine(RepositoryRoot, "schemas", "prompt.schema.json"), Path.Combine(root, "schemas", "prompt.schema.json"));
        File.Copy(Path.Combine(RepositoryRoot, "schemas", "catalog.schema.json"), Path.Combine(root, "schemas", "catalog.schema.json"));
        File.WriteAllText(Path.Combine(root, "prompts", "catalog.yaml"), """
            package: DevComputaria.Prompts
            schema: 1
            aliases:
              example.demo: 1.0.0
            prompts:
              - id: example.demo
                versions: [1.0.0]
            """);
        File.WriteAllText(Path.Combine(root, "prompts", "example", "demo", "1.0.0.yaml"), """
            id: example.demo
            version: 1.0.0
            includes: [_shared/missing]
            parts:
              - role: user
                template: Hello
            """);
    }
}
