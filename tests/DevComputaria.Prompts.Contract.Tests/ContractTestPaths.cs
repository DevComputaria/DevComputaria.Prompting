namespace DevComputaria.Prompts.Contract.Tests;

internal static class ContractTestPaths
{
    public static string RepositoryRoot { get; } = FindRepositoryRoot();

    public static string ResolveFromRoot(params string[] segments)
        => Path.Combine(new[] { RepositoryRoot }.Concat(segments).ToArray());

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

        throw new InvalidOperationException("Repository root could not be located from the test runtime directory.");
    }
}