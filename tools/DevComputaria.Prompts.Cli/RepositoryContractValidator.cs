using DevComputaria.Prompts.Catalogs;

namespace DevComputaria.Prompts.Cli;

internal sealed class RepositoryContractValidator
{
    private readonly SchemaSubsetValidator _schemaValidator = new();
    private readonly YamlPromptLoader _promptLoader = new();

    public IReadOnlyList<string> Validate(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);

        var issues = new List<string>();
        var promptsDirectory = Path.Combine(root, "prompts");
        var catalogPath = Path.Combine(promptsDirectory, "catalog.yaml");
        var promptSchemaPath = Path.Combine(root, "schemas", "prompt.schema.json");
        var catalogSchemaPath = Path.Combine(root, "schemas", "catalog.schema.json");

        if (!File.Exists(promptSchemaPath) || !File.Exists(catalogSchemaPath))
        {
            issues.Add("Required schemas/prompt.schema.json or schemas/catalog.schema.json file is missing.");
            return issues;
        }

        ValidateSchema(catalogSchemaPath, catalogPath, "prompts/catalog.yaml", issues);

        PromptManifest? manifest = null;
        try
        {
            manifest = PromptManifest.Load(File.ReadAllText(catalogPath));
        }
        catch (Exception exception)
        {
            issues.Add($"prompts/catalog.yaml: cannot load manifest: {exception.Message}");
        }

        var promptFiles = Directory
            .EnumerateFiles(promptsDirectory, "*.yaml", SearchOption.AllDirectories)
            .Where(path => !Path.GetFullPath(path).Equals(Path.GetFullPath(catalogPath), StringComparison.Ordinal))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        foreach (var promptPath in promptFiles)
        {
            var relativePath = Path.GetRelativePath(root, promptPath).Replace(Path.DirectorySeparatorChar, '/');
            ValidateSchema(promptSchemaPath, promptPath, relativePath, issues);
            ValidatePrompt(root, promptPath, relativePath, issues);
        }

        if (manifest is not null)
        {
            ValidateManifest(manifest, root, promptFiles, issues);
        }

        return issues;
    }

    private void ValidateSchema(string schemaPath, string yamlPath, string displayPath, ICollection<string> issues)
    {
        try
        {
            foreach (var error in _schemaValidator.ValidateYaml(File.ReadAllText(schemaPath), File.ReadAllText(yamlPath)))
            {
                issues.Add($"{displayPath}: {error}");
            }
        }
        catch (Exception exception)
        {
            issues.Add($"{displayPath}: schema validation could not run: {exception.Message}");
        }
    }

    private void ValidatePrompt(string root, string path, string displayPath, ICollection<string> issues)
    {
        try
        {
            var prompt = _promptLoader.Load(
                File.ReadAllText(path),
                from => File.ReadAllText(ResolveSiblingMarkdown(path, from)));
            var relativeToPrompts = Path.GetRelativePath(Path.Combine(root, "prompts"), path)
                .Replace(Path.DirectorySeparatorChar, '/');

            if (!relativeToPrompts.StartsWith("_shared/", StringComparison.Ordinal))
            {
                var segments = relativeToPrompts.Split('/');
                if (segments.Length != 3)
                {
                    issues.Add($"{displayPath}: path must follow prompts/{{domain}}/{{slug}}/{{version}}.yaml.");
                }
                else
                {
                    var expectedId = $"{segments[0]}.{segments[1]}";
                    var expectedVersion = Path.GetFileNameWithoutExtension(segments[2]);
                    if (!prompt.Id.Name.Equals(expectedId, StringComparison.Ordinal) ||
                        !prompt.Id.Version.Equals(expectedVersion, StringComparison.Ordinal))
                    {
                        issues.Add($"{displayPath}: identity '{prompt.Id}' does not match path '{expectedId}@{expectedVersion}'.");
                    }
                }
            }

            foreach (var include in prompt.Includes)
            {
                var includePath = Path.Combine(root, "prompts", include.Replace('/', Path.DirectorySeparatorChar) + ".yaml");
                if (!File.Exists(includePath))
                {
                    issues.Add($"{displayPath}: include '{include}' was not found at '{Path.GetRelativePath(root, includePath)}'.");
                }
            }

            if (!string.IsNullOrWhiteSpace(prompt.OutputSchemaRef))
            {
                var schemaPath = Path.Combine(root, prompt.OutputSchemaRef.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(schemaPath))
                {
                    issues.Add($"{displayPath}: output schema '{prompt.OutputSchemaRef}' was not found.");
                }
            }

            foreach (var skill in prompt.Skills)
            {
                var skillPath = Path.Combine(
                    root,
                    "skills",
                    skill.Id.Replace('.', Path.DirectorySeparatorChar),
                    skill.Version + ".yaml");
                if (!File.Exists(skillPath))
                {
                    issues.Add($"{displayPath}: pinned skill '{skill.Id}@{skill.Version}' was not found.");
                }
            }
        }
        catch (Exception exception)
        {
            issues.Add($"{displayPath}: cannot load prompt: {exception.Message}");
        }
    }

    private static string ResolveSiblingMarkdown(string promptPath, string from)
    {
        if (string.IsNullOrWhiteSpace(from) || !from.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ||
            Path.GetFileName(from) != from || from.Contains("..", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Prompt part reference '{from}' must name a sibling Markdown file.");
        }

        var resolved = Path.Combine(Path.GetDirectoryName(promptPath)!, from);
        if (!File.Exists(resolved))
        {
            throw new FileNotFoundException($"Prompt part '{from}' was not found.", resolved);
        }

        return resolved;
    }

    private static void ValidateManifest(
        PromptManifest manifest,
        string root,
        IEnumerable<string> promptFiles,
        ICollection<string> issues)
    {
        var promptsDirectory = Path.Combine(root, "prompts");
        var resourceNames = promptFiles
            .Select(path => Path.GetRelativePath(promptsDirectory, path).Replace(Path.DirectorySeparatorChar, '/'))
            .Select(relative => "prompts." + relative)
            .Append(PromptResourceNames.Catalog)
            .Concat(Directory.Exists(Path.Combine(root, "skills"))
                ? Directory.EnumerateFiles(Path.Combine(root, "skills"), "*.yaml", SearchOption.AllDirectories)
                    .Select(path => "skills." + Path.GetRelativePath(Path.Combine(root, "skills"), path).Replace(Path.DirectorySeparatorChar, '/'))
                : Array.Empty<string>());

        try
        {
            foreach (var issue in new PromptManifestConsistencyValidator().Validate(manifest, resourceNames))
            {
                issues.Add($"prompts/catalog.yaml: {issue}");
            }
        }
        catch (Exception exception)
        {
            issues.Add($"prompts/catalog.yaml: consistency validation could not run: {exception.Message}");
        }
    }
}
