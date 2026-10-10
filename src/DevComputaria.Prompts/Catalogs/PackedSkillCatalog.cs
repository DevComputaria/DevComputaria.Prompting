using System.Reflection;
using DevComputaria.PromptKit.Abstractions;

namespace DevComputaria.Prompts.Catalogs;

public sealed class PackedSkillCatalog
{
    private readonly IReadOnlyDictionary<(string Id, string Version), SkillSpec> _skills;

    public PackedSkillCatalog(Assembly assembly, PromptManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(manifest);

        var loader = new YamlSkillLoader();
        _skills = manifest.ExpandSkillVersions().ToDictionary(
            entry => (entry.Id, entry.Version),
            entry => Load(assembly, loader, entry));
    }

    public SkillSpec Get(SkillReference reference)
    {
        ArgumentNullException.ThrowIfNull(reference);
        return _skills.TryGetValue((reference.Id, reference.Version), out var skill)
            ? skill
            : throw new InvalidOperationException($"Pinned skill '{reference.Id}@{reference.Version}' is not published in the packed catalog.");
    }

    private static SkillSpec Load(Assembly assembly, YamlSkillLoader loader, PromptManifest.ManifestSkillVersion entry)
    {
        using var stream = assembly.GetManifestResourceStream(entry.ResourceName)
                           ?? throw new InvalidOperationException($"Embedded skill resource '{entry.ResourceName}' was not found.");
        var skill = loader.Load(stream);
        if (!skill.Id.Equals(entry.Id, StringComparison.Ordinal) || !skill.Version.Equals(entry.Version, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Skill resource '{entry.ResourceName}' identity does not match '{entry.Id}@{entry.Version}'.");
        }

        return skill;
    }
}
