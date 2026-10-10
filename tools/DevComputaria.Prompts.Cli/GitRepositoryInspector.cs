using System.Diagnostics;
using System.Text.RegularExpressions;

namespace DevComputaria.Prompts.Cli;

internal sealed partial class GitRepositoryInspector
{
    public (IReadOnlyList<RepositoryChange> Changes, IReadOnlyList<string> BaseArtifactPaths) Compare(string root, string baseRef)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseRef);

        if (!SafeRefRegex().IsMatch(baseRef))
        {
            throw new ArgumentException($"Git base ref '{baseRef}' contains unsupported characters.", nameof(baseRef));
        }

        var diff = RunGit(root, "diff", "--name-status", "--find-renames", $"{baseRef}...HEAD", "--");
        var tree = RunGit(root, "ls-tree", "-r", "--name-only", baseRef, "--", "prompts", "skills", "schemas/output");

        return (ParseChanges(diff), SplitLines(tree));
    }

    private static IReadOnlyList<RepositoryChange> ParseChanges(string output)
    {
        var changes = new List<RepositoryChange>();
        foreach (var line in SplitLines(output))
        {
            var fields = line.Split('\t');
            if (fields.Length == 2)
            {
                changes.Add(new RepositoryChange(fields[0], fields[1]));
            }
            else if (fields.Length == 3 && fields[0].StartsWith('R'))
            {
                changes.Add(new RepositoryChange(fields[0], fields[2], fields[1]));
            }
            else
            {
                throw new InvalidOperationException($"Unsupported git diff entry: '{line}'.");
            }
        }

        return changes;
    }

    private static string RunGit(string root, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("git")
        {
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
                            ?? throw new InvalidOperationException("Could not start git.");
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"git {arguments[0]} failed: {error.Trim()}");
        }

        return output;
    }

    private static IReadOnlyList<string> SplitLines(string value)
        => value.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    [GeneratedRegex("^[A-Za-z0-9._/-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeRefRegex();
}
