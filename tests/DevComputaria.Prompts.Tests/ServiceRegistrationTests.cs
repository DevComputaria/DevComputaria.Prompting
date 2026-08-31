using DevComputaria.PromptKit.Abstractions;
using DevComputaria.PromptKit.Catalogs;
using DevComputaria.PromptKit.Hosting;
using DevComputaria.Prompts.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace DevComputaria.Prompts.Tests;

public sealed class ServiceRegistrationTests
{
    [Fact]
    public async Task AddPromptKit_ShouldRegisterCoreRuntimeServices()
    {
        var prompt = new PromptSpec(
            new PromptId("image-analysis.analyze-document", "1.0.0"),
            new[] { new RenderedMessage("system", "Country={{country}}") },
            new[] { new PromptVariableSpec("country", isRequired: true, type: "string") });

        var services = new ServiceCollection();
        services.AddPromptKit();
        services.AddSingleton<IPromptCatalog>(new InMemoryPromptCatalog(new[] { prompt }));

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IPromptComposer>());
        Assert.NotNull(provider.GetRequiredService<IPromptSanitizer>());
        Assert.NotNull(provider.GetRequiredService<IPromptRenderer>());

        var renderer = provider.GetRequiredService<IPromptRenderer>();
        var rendered = await renderer.RenderAsync(prompt.Id, new PromptArgs(new Dictionary<string, object?>
        {
            ["country"] = "BR"
        }));

        Assert.Equal("Country=BR", rendered.Messages[0].Content);
    }

    [Fact]
    public async Task AddPackedPrompts_ShouldResolveEssentialServicesInStandardScenario()
    {
        var services = new ServiceCollection();
        services.AddPackedPrompts();

        using var provider = services.BuildServiceProvider();

        var catalog = provider.GetRequiredService<IPromptCatalog>();
        var renderer = provider.GetRequiredService<IPromptRenderer>();
        var packedOptions = provider.GetRequiredService<IOptions<PackedPromptsOptions>>().Value;
        var promptKitOptions = provider.GetRequiredService<IOptions<PromptKitOptions>>().Value;

        var promptId = new PromptId("image-analysis.analyze-document", "1.0.0");
        var resolved = await catalog.GetAsync(promptId);
        var rendered = await renderer.RenderAsync(promptId, new PromptArgs(new Dictionary<string, object?>
        {
            ["country"] = "BR",
            ["document_type"] = "identity-card",
            ["ocr_text"] = "OCR TEXT"
        }));

        Assert.Equal(promptId, resolved.Id);
        Assert.Equal(promptId, rendered.Id);
        Assert.True(packedOptions.StrictPins);
        Assert.True(promptKitOptions.StrictPins);
    }

    [Fact]
    public void AddPackedPrompts_ShouldIgnoreDirectoryOverrideOutsideDevelopment()
    {
        var services = new ServiceCollection();
        services.AddPackedPrompts(options =>
        {
            options.EnvironmentName = "Production";
            options.AllowDirectoryOverride = true;
            options.DirectoryOverridePath = "/tmp/prompts";
        });

        using var provider = services.BuildServiceProvider();

        var packedOptions = provider.GetRequiredService<IOptions<PackedPromptsOptions>>().Value;
        var promptKitOptions = provider.GetRequiredService<IOptions<PromptKitOptions>>().Value;

        Assert.False(packedOptions.AllowDirectoryOverride);
        Assert.Null(packedOptions.DirectoryOverridePath);
        Assert.False(promptKitOptions.AllowDirectoryOverride);
        Assert.Null(promptKitOptions.DirectoryOverridePath);
    }
}