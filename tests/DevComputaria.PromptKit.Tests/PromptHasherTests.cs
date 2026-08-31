using DevComputaria.PromptKit.Abstractions;
using DevComputaria.PromptKit.Hashing;
using Xunit;

namespace DevComputaria.PromptKit.Tests;

public sealed class PromptHasherTests
{
    private readonly PromptHasher _hasher = new();

    [Fact]
    public void ComputeHash_ShouldReturnSameHashForSameLogicalInput()
    {
        var prompt = CreatePromptSpec();
        var argsA = new PromptArgs(new Dictionary<string, object?>
        {
            ["country"] = "BR",
            ["document_type"] = "identity-card"
        });
        var argsB = new PromptArgs(new Dictionary<string, object?>
        {
            ["document_type"] = "identity-card",
            ["country"] = "BR"
        });
        var renderedMessages = new[]
        {
            new RenderedMessage("system", "Answer only JSON. Country=BR"),
            new RenderedMessage("user", "Type=identity-card")
        };

        var hashA = _hasher.ComputeHash(prompt, argsA, renderedMessages);
        var hashB = _hasher.ComputeHash(prompt, argsB, renderedMessages);

        Assert.Equal(hashA, hashB);
        Assert.Equal(64, hashA.Length);
    }

    [Fact]
    public void ComputeHash_ShouldChangeWhenRelevantContentChanges()
    {
        var prompt = CreatePromptSpec();
        var args = new PromptArgs(new Dictionary<string, object?>
        {
            ["country"] = "BR",
            ["document_type"] = "identity-card"
        });
        var renderedMessagesA = new[]
        {
            new RenderedMessage("system", "Answer only JSON. Country=BR"),
            new RenderedMessage("user", "Type=identity-card")
        };
        var renderedMessagesB = new[]
        {
            new RenderedMessage("system", "Answer only JSON. Country=US"),
            new RenderedMessage("user", "Type=identity-card")
        };

        var hashA = _hasher.ComputeHash(prompt, args, renderedMessagesA);
        var hashB = _hasher.ComputeHash(prompt, args, renderedMessagesB);

        Assert.NotEqual(hashA, hashB);
    }

    [Fact]
    public void ComputeHash_ShouldBeStableAcrossInstancesForSameInput()
    {
        var prompt = CreatePromptSpec();
        var args = new PromptArgs(new Dictionary<string, object?>
        {
            ["country"] = "BR",
            ["document_type"] = "identity-card"
        });
        var renderedMessages = new[]
        {
            new RenderedMessage("system", "Answer only JSON. Country=BR"),
            new RenderedMessage("user", "Type=identity-card")
        };

        var hashA = new PromptHasher().ComputeHash(prompt, args, renderedMessages);
        var hashB = new PromptHasher().ComputeHash(prompt, args, renderedMessages);

        Assert.Equal(hashA, hashB);
        Assert.Equal("cad28e835fe29d85b5be746cbe2f9eb47d5bd2de5eaf91419f9685d2927402f6", hashA);
    }

    private static PromptSpec CreatePromptSpec()
    {
        return new PromptSpec(
            new PromptId("image-analysis.analyze-document", "1.0.0"),
            new[]
            {
                new RenderedMessage("system", "Answer only JSON. {{#if country}}Country={{country}}{{/if}}"),
                new RenderedMessage("user", "Type={{document_type}}")
            },
            new[]
            {
                new PromptVariableSpec("country", isRequired: true, type: "string"),
                new PromptVariableSpec("document_type", isRequired: true, type: "string")
            },
            includes: new[] { "_shared/json-only" },
            outputSchemaRef: "schemas/output/image-analysis-document-v1.json");
    }
}