namespace DevComputaria.Prompts.Cli;

internal static class RepositoryPaths
{
    public static string Resolve(string? explicitRoot)
    {
        if (!string.IsNullOrWhiteSpace(explicitRoot))
        {
            return EnsureRoot(Path.GetFullPath(explicitRoot));
        }

        foreach (var startingPath in new[] { Environment.CurrentDirectory, AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(startingPath);
            while (directory is not null)
            {
                if (IsRoot(directory.FullName))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }
        }

        throw new DirectoryNotFoundException("Repository root was not found. Use --root <repository>.");
    }

    private static string EnsureRoot(string path)
        => IsRoot(path)
            ? path
            : throw new DirectoryNotFoundException($"'{path}' is not a prompting repository root (expected prompts/catalog.yaml and schemas/).");

    private static bool IsRoot(string path)
        => File.Exists(Path.Combine(path, "prompts", "catalog.yaml"))
           && Directory.Exists(Path.Combine(path, "schemas"));
}
