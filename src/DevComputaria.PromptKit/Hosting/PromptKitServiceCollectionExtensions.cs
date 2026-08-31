using DevComputaria.PromptKit.Abstractions;
using DevComputaria.PromptKit.Composition;
using DevComputaria.PromptKit.Hashing;
using DevComputaria.PromptKit.Rendering;
using DevComputaria.PromptKit.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace DevComputaria.PromptKit.Hosting;

public static class PromptKitServiceCollectionExtensions
{
    public static IServiceCollection AddPromptKit(this IServiceCollection services, Action<PromptKitOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new PromptKitOptions();
        configure?.Invoke(options);
        options = Normalize(options);

        services.TryAddSingleton<IOptions<PromptKitOptions>>(_ => Options.Create(options));
        services.TryAddSingleton<PromptHasher>();
        services.TryAddSingleton<TemplateSandbox>();
        services.TryAddSingleton<IPromptComposer, PassthroughPromptComposer>();
        services.TryAddSingleton<IPromptSanitizer, VariableValidator>();
        services.TryAddSingleton<IPromptRenderer>(serviceProvider =>
            new HandlebarsPromptRenderer(
                serviceProvider.GetRequiredService<IPromptCatalog>(),
                serviceProvider.GetRequiredService<IPromptComposer>(),
                serviceProvider.GetRequiredService<IPromptSanitizer>(),
                serviceProvider.GetRequiredService<TemplateSandbox>(),
                serviceProvider.GetRequiredService<PromptHasher>()));

        return services;
    }

    private static PromptKitOptions Normalize(PromptKitOptions options)
    {
        if (!IsDevelopment(options.EnvironmentName))
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

    private static bool IsDevelopment(string? environmentName)
        => string.Equals(environmentName?.Trim(), "Development", StringComparison.OrdinalIgnoreCase);
}