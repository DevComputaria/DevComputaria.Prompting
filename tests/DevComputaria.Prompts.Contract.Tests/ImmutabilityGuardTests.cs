using System.Security.Cryptography;
using System.Text.Json;
using Xunit;

namespace DevComputaria.Prompts.Contract.Tests;

public sealed class ImmutabilityGuardTests
{
    [Fact]
    public void PublishedArtifacts_ShouldMatchLockedHashes()
    {
        var baselinePath = ContractTestPaths.ResolveFromRoot("tests", "DevComputaria.Prompts.Contract.Tests", "Baselines", "published-artifacts.lock.json");
        var baseline = JsonSerializer.Deserialize<PublishedArtifactsLock>(File.ReadAllText(baselinePath))
                       ?? throw new InvalidOperationException("Published artifacts baseline could not be deserialized.");

        var mismatches = new List<string>();
        foreach (var artifact in baseline.Artifacts)
        {
            var path = ContractTestPaths.ResolveFromRoot(artifact.Path.Replace('/', Path.DirectorySeparatorChar));
            var actualHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
            if (!string.Equals(actualHash, artifact.Sha256, StringComparison.Ordinal))
            {
                mismatches.Add($"{artifact.Path}: expected {artifact.Sha256}, actual {actualHash}");
            }
        }

        Assert.True(mismatches.Count == 0, "Published artifact immutability mismatch detected: " + string.Join(" | ", mismatches));
    }

    private sealed class PublishedArtifactsLock
    {
        public List<PublishedArtifact> Artifacts { get; set; } = new();
    }

    private sealed class PublishedArtifact
    {
        public string Path { get; set; } = string.Empty;

        public string Sha256 { get; set; } = string.Empty;
    }
}