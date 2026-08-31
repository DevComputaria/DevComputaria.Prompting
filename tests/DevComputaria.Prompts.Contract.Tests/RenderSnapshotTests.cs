using System.Text.Json;
using DevComputaria.PromptKit.Abstractions;
using DevComputaria.Prompts.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DevComputaria.Prompts.Contract.Tests;

public sealed class RenderSnapshotTests
{
    [Fact]
    public async Task PackedPromptRender_ShouldMatchApprovedSnapshot()
    {
        var snapshotPath = ContractTestPaths.ResolveFromRoot("tests", "DevComputaria.Prompts.Contract.Tests", "Baselines", "render-image-analysis-analyze-document-1.0.0.json");
        var snapshot = JsonSerializer.Deserialize<RenderSnapshot>(
            File.ReadAllText(snapshotPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                       ?? throw new InvalidOperationException("Render snapshot baseline could not be deserialized.");

        var services = new ServiceCollection();
        services.AddPackedPrompts();

        using var provider = services.BuildServiceProvider();
        var renderer = provider.GetRequiredService<IPromptRenderer>();

        var promptId = new PromptId(snapshot.PromptId, snapshot.Version);
        var rendered = await renderer.RenderAsync(promptId, new PromptArgs(snapshot.Args.Select(x => new KeyValuePair<string, object?>(x.Key, x.Value))));

        Assert.Equal(snapshot.ContentSha256, rendered.ContentSha256);
        Assert.Equal(snapshot.Messages.Count, rendered.Messages.Count);

        for (var i = 0; i < snapshot.Messages.Count; i++)
        {
            Assert.Equal(snapshot.Messages[i].Role, rendered.Messages[i].Role);
            Assert.Equal(snapshot.Messages[i].Content, rendered.Messages[i].Content);
        }
    }

    private sealed class RenderSnapshot
    {
        public string PromptId { get; set; } = string.Empty;

        public string Version { get; set; } = string.Empty;

        public Dictionary<string, string> Args { get; set; } = new(StringComparer.Ordinal);

        public string ContentSha256 { get; set; } = string.Empty;

        public List<RenderedMessageSnapshot> Messages { get; set; } = new();
    }

    private sealed class RenderedMessageSnapshot
    {
        public string Role { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;
    }
}