using Consumer.ImageAnalysis;
using DevComputaria.PromptKit.Abstractions;
using DevComputaria.Prompts.Hosting;
using DevComputaria.Prompts.Ids;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

var environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
var directoryOverride = Environment.GetEnvironmentVariable("PROMPTS_DIRECTORY_OVERRIDE");

var services = new ServiceCollection();
services.AddPackedPrompts(options =>
{
    options.EnvironmentName = environmentName;
    options.AllowDirectoryOverride = !string.IsNullOrWhiteSpace(directoryOverride);
    options.DirectoryOverridePath = directoryOverride;
});
services.AddSingleton<IChatClient, FakeChatClient>();

using var provider = services.BuildServiceProvider();
var renderer = provider.GetRequiredService<IPromptRenderer>();
var chatClient = provider.GetRequiredService<IChatClient>();
var promptOptions = provider.GetRequiredService<IOptions<PackedPromptsOptions>>().Value;

if (!environmentName.Equals("Development", StringComparison.OrdinalIgnoreCase) &&
    promptOptions.AllowDirectoryOverride)
{
    throw new InvalidOperationException("Directory override must never be enabled outside Development.");
}

var promptId = PromptPins.ImageAnalysisAnalyzeDocument.V1_1_0;
var promptArgs = new PromptArgs(new Dictionary<string, object?>
{
    ["country"] = "BR",
    ["document_type"] = "identity-card",
    ["ocr_text"] = args.Length > 0 ? args[0] : "OCR-123"
});

var rendered = await renderer.RenderAsync(promptId, promptArgs);
var completion = await chatClient.CompleteAsync(rendered);

Console.WriteLine($"Environment: {environmentName}");
Console.WriteLine($"Directory override enabled: {promptOptions.AllowDirectoryOverride}");
Console.WriteLine($"Prompt: {rendered.Id}");
Console.WriteLine($"SHA-256: {rendered.ContentSha256}");
Console.WriteLine($"Package version: {rendered.PackageVersion ?? "not-reported"}");
Console.WriteLine($"Renderer: {rendered.Hints.GetValueOrDefault("renderer") ?? "not-reported"}");
Console.WriteLine("Messages:");

foreach (var message in rendered.Messages)
{
    Console.WriteLine($"[{message.Role}]");
    Console.WriteLine(message.Content);
}

Console.WriteLine("Tools:");
foreach (var tool in rendered.Tools)
{
    Console.WriteLine($"- {tool.Name}: {tool.Description}");
}

Console.WriteLine($"Fake client: {completion.Provider}");
Console.WriteLine($"Fake response: {completion.Content}");
