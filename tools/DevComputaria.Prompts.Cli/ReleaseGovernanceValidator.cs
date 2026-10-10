using System.Text.RegularExpressions;

namespace DevComputaria.Prompts.Cli;

public enum SemVersionImpact
{
    None = 0,
    Patch = 1,
    Minor = 2,
    Major = 3
}

public sealed record RepositoryChange(string Status, string Path, string? PreviousPath = null);

public sealed record ReleaseGovernanceResult(SemVersionImpact RequiredImpact, IReadOnlyList<string> Issues)
{
    public bool IsValid => Issues.Count == 0;
}

public sealed partial class ReleaseGovernanceValidator
{
    public ReleaseGovernanceResult Validate(
        IEnumerable<RepositoryChange> changes,
        IEnumerable<string> baseArtifactPaths,
        SemVersionImpact declaredImpact,
        string? previousPackageVersion = null,
        string? nextPackageVersion = null)
    {
        ArgumentNullException.ThrowIfNull(changes);
        ArgumentNullException.ThrowIfNull(baseArtifactPaths);

        if (declaredImpact is SemVersionImpact.None)
        {
            throw new ArgumentException("A PATCH, MINOR, or MAJOR impact must be declared.", nameof(declaredImpact));
        }

        var issues = new List<string>();
        var required = SemVersionImpact.None;
        var baseArtifacts = baseArtifactPaths
            .Select(NormalizePath)
            .Select(ParseArtifact)
            .Where(artifact => artifact is not null)
            .Cast<VersionedArtifact>()
            .ToArray();

        foreach (var change in ExpandRenames(changes))
        {
            var path = NormalizePath(change.Path);
            var artifact = ParseArtifact(path);

            if (artifact is not null)
            {
                switch (change.Status)
                {
                    case "M":
                        issues.Add($"Published artifact '{path}' cannot be modified in place; add a new versioned path.");
                        break;
                    case "D":
                        required = Max(required, SemVersionImpact.Major);
                        break;
                    case "A":
                        required = Max(required, ClassifyAddition(artifact, baseArtifacts, issues));
                        break;
                }

                continue;
            }

            required = Max(required, ClassifyRepositoryChange(change.Status, path));
        }

        if (required is SemVersionImpact.None)
        {
            required = SemVersionImpact.Patch;
        }

        if (declaredImpact < required)
        {
            issues.Add($"Declared {declaredImpact.ToString().ToUpperInvariant()} impact is lower than required {required.ToString().ToUpperInvariant()} impact.");
        }

        ValidatePackageVersions(previousPackageVersion, nextPackageVersion, declaredImpact, issues);
        return new ReleaseGovernanceResult(required, issues.AsReadOnly());
    }

    public static SemVersionImpact ParseImpact(string value)
        => value.Trim().ToLowerInvariant() switch
        {
            "patch" => SemVersionImpact.Patch,
            "minor" => SemVersionImpact.Minor,
            "major" => SemVersionImpact.Major,
            _ => throw new ArgumentException($"Unknown SemVer impact '{value}'. Expected patch, minor, or major.", nameof(value))
        };

    private static IEnumerable<RepositoryChange> ExpandRenames(IEnumerable<RepositoryChange> changes)
    {
        foreach (var change in changes)
        {
            if (change.Status.StartsWith('R') && change.PreviousPath is not null)
            {
                yield return new RepositoryChange("D", change.PreviousPath);
                yield return new RepositoryChange("A", change.Path);
            }
            else
            {
                yield return change with { Status = change.Status[..1].ToUpperInvariant() };
            }
        }
    }

    private static SemVersionImpact ClassifyAddition(
        VersionedArtifact artifact,
        IReadOnlyList<VersionedArtifact> baseArtifacts,
        ICollection<string> issues)
    {
        var previousVersions = baseArtifacts
            .Where(candidate => candidate.Key.Equals(artifact.Key, StringComparison.Ordinal))
            .Select(candidate => candidate.Version)
            .Distinct()
            .OrderByDescending(version => version)
            .ToArray();

        if (previousVersions.Length == 0)
        {
            return SemVersionImpact.Minor;
        }

        if (previousVersions.Contains(artifact.Version))
        {
            issues.Add($"Artifact component '{artifact.Path}' was added to already published version '{artifact.Version}'. Create a new version instead.");
            return SemVersionImpact.Patch;
        }

        var previous = previousVersions[0];
        if (artifact.Version <= previous)
        {
            issues.Add($"Artifact '{artifact.Path}' must advance beyond published version '{previous}'.");
            return SemVersionImpact.Patch;
        }

        if (artifact.Kind == "schema")
        {
            return SemVersionImpact.Major;
        }

        if (artifact.Version.Major > previous.Major)
        {
            return SemVersionImpact.Major;
        }

        return artifact.Version.Minor > previous.Minor
            ? SemVersionImpact.Minor
            : SemVersionImpact.Patch;
    }

    private static SemVersionImpact ClassifyRepositoryChange(string status, string path)
    {
        if (status == "D" && path.StartsWith("src/", StringComparison.Ordinal))
        {
            return SemVersionImpact.Major;
        }

        if (path is "schemas/prompt.schema.json" or "schemas/catalog.schema.json")
        {
            return SemVersionImpact.Major;
        }

        if (path.StartsWith("src/", StringComparison.Ordinal))
        {
            return SemVersionImpact.Minor;
        }

        return SemVersionImpact.Patch;
    }

    private static void ValidatePackageVersions(
        string? previousPackageVersion,
        string? nextPackageVersion,
        SemVersionImpact declaredImpact,
        ICollection<string> issues)
    {
        if (previousPackageVersion is null && nextPackageVersion is null)
        {
            return;
        }

        if (!SemanticVersion.TryParse(previousPackageVersion, out var previous) ||
            !SemanticVersion.TryParse(nextPackageVersion, out var next))
        {
            issues.Add("Both --previous-version and --next-version must use the X.Y.Z format.");
            return;
        }

        var actualImpact = next.Major > previous.Major
            ? SemVersionImpact.Major
            : next.Major == previous.Major && next.Minor > previous.Minor
                ? SemVersionImpact.Minor
                : next.Major == previous.Major && next.Minor == previous.Minor && next.Patch > previous.Patch
                    ? SemVersionImpact.Patch
                    : SemVersionImpact.None;

        if (actualImpact is SemVersionImpact.None)
        {
            issues.Add($"Next package version '{next}' must be greater than previous version '{previous}'.");
        }
        else if (actualImpact != declaredImpact)
        {
            issues.Add($"Package version transition '{previous}' -> '{next}' is {actualImpact.ToString().ToUpperInvariant()}, not declared {declaredImpact.ToString().ToUpperInvariant()}.");
        }
    }

    private static VersionedArtifact? ParseArtifact(string path)
    {
        var promptOrSkill = PromptOrSkillRegex().Match(path);
        if (promptOrSkill.Success && SemanticVersion.TryParse(promptOrSkill.Groups[4].Value, out var version))
        {
            return new VersionedArtifact(
                path,
                $"{promptOrSkill.Groups[1].Value}/{promptOrSkill.Groups[2].Value}/{promptOrSkill.Groups[3].Value}",
                promptOrSkill.Groups[1].Value,
                version);
        }

        var schema = OutputSchemaRegex().Match(path);
        if (schema.Success && int.TryParse(schema.Groups[2].Value, out var schemaVersion))
        {
            return new VersionedArtifact(
                path,
                $"schemas/output/{schema.Groups[1].Value}",
                "schema",
                new SemanticVersion(schemaVersion, 0, 0));
        }

        return null;
    }

    private static SemVersionImpact Max(SemVersionImpact left, SemVersionImpact right)
        => left >= right ? left : right;

    private static string NormalizePath(string path) => path.Replace('\\', '/').TrimStart('.', '/');

    [GeneratedRegex("^(prompts|skills)/([^/]+)/([^/]+)/(\\d+\\.\\d+\\.\\d+)(?:\\.[^/]+)?\\.(?:yaml|md)$", RegexOptions.CultureInvariant)]
    private static partial Regex PromptOrSkillRegex();

    [GeneratedRegex("^schemas/output/(.+)-v(\\d+)\\.json$", RegexOptions.CultureInvariant)]
    private static partial Regex OutputSchemaRegex();

    private sealed record VersionedArtifact(string Path, string Key, string Kind, SemanticVersion Version);

    private readonly record struct SemanticVersion(int Major, int Minor, int Patch) : IComparable<SemanticVersion>
    {
        public static bool TryParse(string? value, out SemanticVersion version)
        {
            var parts = value?.Split('.');
            if (parts is { Length: 3 } &&
                int.TryParse(parts[0], out var major) &&
                int.TryParse(parts[1], out var minor) &&
                int.TryParse(parts[2], out var patch) &&
                major >= 0 && minor >= 0 && patch >= 0)
            {
                version = new SemanticVersion(major, minor, patch);
                return true;
            }

            version = default;
            return false;
        }

        public int CompareTo(SemanticVersion other)
        {
            var major = Major.CompareTo(other.Major);
            if (major != 0) return major;
            var minor = Minor.CompareTo(other.Minor);
            return minor != 0 ? minor : Patch.CompareTo(other.Patch);
        }

        public static bool operator >(SemanticVersion left, SemanticVersion right) => left.CompareTo(right) > 0;
        public static bool operator <(SemanticVersion left, SemanticVersion right) => left.CompareTo(right) < 0;
        public static bool operator >=(SemanticVersion left, SemanticVersion right) => left.CompareTo(right) >= 0;
        public static bool operator <=(SemanticVersion left, SemanticVersion right) => left.CompareTo(right) <= 0;

        public override string ToString() => $"{Major}.{Minor}.{Patch}";
    }
}
