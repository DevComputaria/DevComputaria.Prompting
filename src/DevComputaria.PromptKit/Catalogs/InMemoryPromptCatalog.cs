using DevComputaria.PromptKit.Abstractions;
using DevComputaria.PromptKit.Exceptions;

namespace DevComputaria.PromptKit.Catalogs;

public sealed class InMemoryPromptCatalog : IPromptCatalog
{
    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, PromptSpec>> _index;

    public InMemoryPromptCatalog(IEnumerable<PromptSpec> prompts)
    {
        ArgumentNullException.ThrowIfNull(prompts);

        var byName = new Dictionary<string, Dictionary<string, PromptSpec>>(StringComparer.Ordinal);

        foreach (var prompt in prompts)
        {
            ArgumentNullException.ThrowIfNull(prompt);

            if (!byName.TryGetValue(prompt.Id.Name, out var versions))
            {
                versions = new Dictionary<string, PromptSpec>(StringComparer.Ordinal);
                byName[prompt.Id.Name] = versions;
            }

            if (!versions.TryAdd(prompt.Id.Version, prompt))
            {
                throw new ArgumentException(
                    $"A prompt with id '{prompt.Id}' is already registered in the catalog.",
                    nameof(prompts));
            }
        }

        _index = byName.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyDictionary<string, PromptSpec>)pair.Value,
            StringComparer.Ordinal);
    }

    public ValueTask<PromptSpec> GetAsync(PromptId id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_index.TryGetValue(id.Name, out var versions))
        {
            throw new PromptNotFoundException(id);
        }

        if (!versions.TryGetValue(id.Version, out var prompt))
        {
            throw new PromptVersionMismatchException(id, versions.Keys.OrderBy(x => x, StringComparer.Ordinal));
        }

        return ValueTask.FromResult(prompt);
    }
}