using DevComputaria.PromptKit.Abstractions;
using DevComputaria.PromptKit.Rendering;

namespace DevComputaria.PromptKit.Composition;

public sealed class SkillAttacher
{
    public PromptSpec Attach(PromptSpec prompt, Func<SkillReference, SkillSpec> skillResolver)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentNullException.ThrowIfNull(skillResolver);

        if (prompt.Skills.Count == 0)
        {
            return prompt;
        }

        var parts = prompt.Parts.ToList();
        var tools = prompt.Tools.ToList();

        foreach (var reference in prompt.Skills)
        {
            var skill = skillResolver(reference);
            if (!skill.Id.Equals(reference.Id, StringComparison.Ordinal) ||
                !skill.Version.Equals(reference.Version, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Resolved skill '{skill.Id}@{skill.Version}' does not match pin '{reference.Id}@{reference.Version}'.");
            }

            if (reference.Attach is "system" or "both")
            {
                if (skill.Body is null)
                {
                    throw new InvalidOperationException($"Skill '{reference.Id}@{reference.Version}' has no body to attach.");
                }

                var systemIndex = parts.FindIndex(part => part.Role.Equals("system", StringComparison.Ordinal));
                if (systemIndex >= 0)
                {
                    var system = parts[systemIndex];
                    parts[systemIndex] = new RenderedMessage(
                        system.Role,
                        system.Content + Environment.NewLine + Environment.NewLine + skill.Body,
                        system.Source);
                }
                else
                {
                    parts.Insert(0, new RenderedMessage("system", skill.Body, $"skill:{reference.Id}@{reference.Version}"));
                }
            }

            if (reference.Attach is "tool" or "both")
            {
                tools.AddRange(skill.Tools);
            }
        }

        return SchemaInjector.Copy(prompt, parts, tools: tools);
    }
}
