using System.Text.RegularExpressions;
using DevComputaria.PromptKit.Abstractions;
using DevComputaria.Prompts.Catalogs;
using DevComputaria.Prompts.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DevComputaria.Prompts.Eval.Tests;

public sealed partial class PromptEvalTests
{
    [Fact]
    public void EvalDatasets_ShouldCoverEveryPromptologiaVersion()
    {
        var root = RepositoryRoot.Resolve();
        var manifest = PromptManifest.Load(File.ReadAllText(Path.Combine(root, "prompts", "catalog.yaml")));
        var expected = manifest.ExpandVersions()
            .Where(entry => entry.Id.StartsWith("promptologia.", StringComparison.Ordinal))
            .Select(entry => $"{entry.Id}@{entry.Version}")
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        var actual = LoadDocuments(root)
            .Select(document => $"{document.PromptId}@{document.Version}")
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EvalDatasets_ShouldContainUseCaseAndBoundaryCoverage()
    {
        var root = RepositoryRoot.Resolve();
        foreach (var (path, document) in LoadDocumentsWithPaths(root))
        {
            var expectedDirectory = Path.Combine(root, "evals", document.PromptId);
            var expectedFile = $"{document.Version}.cases.json";
            Assert.Equal(expectedDirectory, Path.GetDirectoryName(path));
            Assert.Equal(expectedFile, Path.GetFileName(path));
            Assert.False(string.IsNullOrWhiteSpace(document.PromptId));
            Assert.Matches("^[a-z0-9.-]+$", document.PromptId);
            Assert.Matches("^\\d+\\.\\d+\\.\\d+$", document.Version);
            Assert.True(document.Cases.Count >= 2, $"{document.PromptId}@{document.Version} must have at least two eval cases.");
            Assert.Contains(document.Cases, testCase => testCase.Category == "happy-path");
            Assert.Contains(document.Cases, testCase => testCase.Category != "happy-path");
            Assert.Equal(document.Cases.Count, document.Cases.Select(testCase => testCase.Id).Distinct(StringComparer.Ordinal).Count());
            Assert.All(document.Cases, testCase =>
            {
                Assert.False(string.IsNullOrWhiteSpace(testCase.Id));
                Assert.False(string.IsNullOrWhiteSpace(testCase.Category));
                Assert.NotEmpty(testCase.RenderMustContain);
                Assert.True(testCase.OutputMustContain.Count + testCase.OutputMustNotContain.Count > 0,
                    $"{document.PromptId}@{document.Version}/{testCase.Id} must define an output oracle.");
            });
        }
    }

    [Fact]
    public async Task EvalCases_ShouldRenderAndSatisfyTheirLocalOracle()
    {
        var services = new ServiceCollection();
        services.AddPackedPrompts();
        using var provider = services.BuildServiceProvider();
        var renderer = provider.GetRequiredService<IPromptRenderer>();
        var catalog = provider.GetRequiredService<IPromptCatalog>();

        foreach (var document in LoadDocuments(RepositoryRoot.Resolve()))
        {
            var promptId = new PromptId(document.PromptId, document.Version);
            var prompt = await catalog.GetAsync(promptId);

            foreach (var testCase in document.Cases)
            {
                var undeclared = testCase.Args.Keys.Except(prompt.Variables.Keys, StringComparer.Ordinal).ToArray();
                Assert.True(undeclared.Length == 0, $"{promptId}/{testCase.Id} has undeclared args: {string.Join(", ", undeclared)}");

                var missing = prompt.Variables.Values
                    .Where(variable => variable.IsRequired)
                    .Select(variable => variable.Name)
                    .Except(testCase.Args.Keys, StringComparer.Ordinal)
                    .ToArray();
                Assert.True(missing.Length == 0, $"{promptId}/{testCase.Id} is missing args: {string.Join(", ", missing)}");

                var args = new PromptArgs(testCase.Args.Keys.Select(key =>
                    new KeyValuePair<string, object?>(key, testCase.GetArgument(key))));
                var rendered = await renderer.RenderAsync(promptId, args);
                var renderedText = string.Join("\n", rendered.Messages.Select(message => message.Content));

                Assert.False(UnresolvedVariableRegex().IsMatch(renderedText), $"{promptId}/{testCase.Id} left an unresolved variable.");
                foreach (var expected in testCase.RenderMustContain)
                {
                    Assert.Contains(expected, renderedText, StringComparison.Ordinal);
                }

                Assert.False(string.IsNullOrWhiteSpace(testCase.CandidateOutput));
                foreach (var expected in testCase.OutputMustContain)
                {
                    Assert.Contains(expected, testCase.CandidateOutput, StringComparison.OrdinalIgnoreCase);
                }

                foreach (var forbidden in testCase.OutputMustNotContain)
                {
                    Assert.DoesNotContain(forbidden, testCase.CandidateOutput, StringComparison.OrdinalIgnoreCase);
                }
            }
        }
    }

    private static IReadOnlyList<PromptEvalCaseDocument> LoadDocuments(string root)
        => LoadDocumentsWithPaths(root).Select(item => item.Document).ToArray();

    private static IReadOnlyList<(string Path, PromptEvalCaseDocument Document)> LoadDocumentsWithPaths(string root)
        => Directory.EnumerateFiles(Path.Combine(root, "evals"), "*.cases.json", SearchOption.AllDirectories)
            .Where(path => Path.GetFileName(Path.GetDirectoryName(path)!).StartsWith("promptologia.", StringComparison.Ordinal))
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(path => (path, PromptEvalCaseDocument.Load(path)))
            .ToArray();

    [GeneratedRegex("\\{\\{\\s*[a-zA-Z0-9_.-]+\\s*\\}\\}", RegexOptions.CultureInvariant)]
    private static partial Regex UnresolvedVariableRegex();
}
