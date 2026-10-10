namespace DevComputaria.PromptKit.Abstractions;

public sealed record SkillSpec
{
    public SkillSpec(string id, string version, string? body = null, IEnumerable<PromptTool>? tools = null)
    {
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Skill id is required.", nameof(id)) : id.Trim();
        Version = string.IsNullOrWhiteSpace(version) ? throw new ArgumentException("Skill version is required.", nameof(version)) : version.Trim();
        Body = string.IsNullOrWhiteSpace(body) ? null : body.Trim();
        Tools = (tools ?? Array.Empty<PromptTool>()).ToArray();
    }

    public string Id { get; }

    public string Version { get; }

    public string? Body { get; }

    public IReadOnlyList<PromptTool> Tools { get; }
}
