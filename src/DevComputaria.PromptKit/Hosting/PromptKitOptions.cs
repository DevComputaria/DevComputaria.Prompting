namespace DevComputaria.PromptKit.Hosting;

public sealed class PromptKitOptions
{
    public bool StrictPins { get; set; } = true;

    public bool AllowDirectoryOverride { get; set; }

    public string? DirectoryOverridePath { get; set; }

    public string EnvironmentName { get; set; } = "Production";
}