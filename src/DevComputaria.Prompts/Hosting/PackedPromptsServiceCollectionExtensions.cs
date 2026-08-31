using DevComputaria.PromptKit.Abstractions;
using DevComputaria.PromptKit.Hosting;
using DevComputaria.Prompts.Catalogs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace DevComputaria.Prompts.Hosting;

public static class PackedPromptsServiceCollectionExtensions
{
    public static IServiceCollection AddPackedPrompts(this IServiceCollection services, Action<PackedPromptsOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new PackedPromptsOptions();
        configure?.Invoke(options);
        options = Normalize(options);

        services.AddPromptKit(promptKitOptions =>
        {
            promptKitOptions.StrictPins = options.StrictPins;
            promptKitOptions.AllowDirectoryOverride = options.AllowDirectoryOverride;
            promptKitOptions.DirectoryOverridePath = options.DirectoryOverridePath;
            promptKitOptions.EnvironmentName = options.EnvironmentName;
        });

        services.TryAddSingleton<IOptions<PackedPromptsOptions>>(_ => Options.Create(options));
        services.TryAddSingleton<IPromptCatalog>(_ => new PackedPromptCatalog(typeof(PackedPromptsServiceCollectionExtensions).Assembly));

        return services;
    }

    private static PackedPromptsOptions Normalize(PackedPromptsOptions options)
    {
        if (!string.Equals(options.EnvironmentName?.Trim(), "Development", StringComparison.OrdinalIgnoreCase))
        {
            options.AllowDirectoryOverride = false;
            options.DirectoryOverridePath = null;
        }
        else if (string.IsNullOrWhiteSpace(options.DirectoryOverridePath))
        {
            options.AllowDirectoryOverride = false;
            options.DirectoryOverridePath = null;
        }
        else
        {
            options.DirectoryOverridePath = options.DirectoryOverridePath.Trim();
        }

        return options;
    }
}