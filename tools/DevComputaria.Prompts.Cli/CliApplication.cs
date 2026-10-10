using System.Text.Json;
using DevComputaria.PromptKit.Abstractions;
using DevComputaria.PromptKit.Composition;
using DevComputaria.PromptKit.Hashing;
using DevComputaria.PromptKit.Rendering;
using DevComputaria.PromptKit.Validation;
using DevComputaria.Prompts.Catalogs;

namespace DevComputaria.Prompts.Cli;

public sealed class CliApplication
{
    private readonly TextWriter _output;
    private readonly TextWriter _error;

    public CliApplication(TextWriter output, TextWriter error)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
        _error = error ?? throw new ArgumentNullException(nameof(error));
    }

    public async Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(args);

        try
        {
            if (args.Length == 0 || IsHelp(args[0]))
            {
                await _output.WriteLineAsync(Usage);
                return args.Length == 0 ? 1 : 0;
            }

            return args[0].ToLowerInvariant() switch
            {
                "validate" => await ValidateAsync(args[1..]),
                "render" => await RenderAsync(args[1..], cancellationToken),
                _ => await FailAsync($"Unknown command '{args[0]}'.{Environment.NewLine}{Usage}")
            };
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return await FailAsync(exception.Message);
        }
    }

    private async Task<int> ValidateAsync(string[] args)
    {
        if (args.Length == 1 && IsHelp(args[0]))
        {
            await _output.WriteLineAsync(ValidateUsage);
            return 0;
        }

        var options = ParseOptions(args, allowArguments: false);
        var root = RepositoryPaths.Resolve(options.Root);
        var issues = new RepositoryContractValidator().Validate(root);

        if (issues.Count > 0)
        {
            await _error.WriteLineAsync($"Contract validation failed with {issues.Count} error(s):");
            foreach (var issue in issues)
            {
                await _error.WriteLineAsync($"- {issue}");
            }

            return 1;
        }

        await _output.WriteLineAsync($"Contract validation succeeded: {root}");
        return 0;
    }

    private async Task<int> RenderAsync(string[] args, CancellationToken cancellationToken)
    {
        if (args.Length == 0 || IsHelp(args[0]))
        {
            await _output.WriteLineAsync(RenderUsage);
            return args.Length == 0 ? 1 : 0;
        }

        var pin = args[0].Split('@', 2, StringSplitOptions.TrimEntries);
        if (pin.Length != 2 || pin.Any(string.IsNullOrWhiteSpace))
        {
            return await FailAsync("A pinned prompt is required in the '<id>@<version>' format.");
        }

        var options = ParseOptions(args[1..], allowArguments: true);
        var values = new Dictionary<string, object?>(StringComparer.Ordinal);

        if (options.ArgsFile is not null)
        {
            foreach (var pair in ReadArguments(options.ArgsFile))
            {
                values[pair.Key] = pair.Value;
            }
        }

        foreach (var argument in options.Arguments)
        {
            var separator = argument.IndexOf('=');
            if (separator <= 0)
            {
                return await FailAsync($"Invalid --arg '{argument}'. Expected key=value.");
            }

            values[argument[..separator].Trim()] = argument[(separator + 1)..];
        }

        var catalog = new PackedPromptCatalog(typeof(PackedPromptCatalog).Assembly);
        var renderer = new HandlebarsPromptRenderer(
            catalog,
            new PassthroughPromptComposer(),
            new VariableValidator(),
            new TemplateSandbox(),
            new PromptHasher());

        var rendered = await renderer.RenderAsync(
            new PromptId(pin[0], pin[1]),
            new PromptArgs(values),
            cancellationToken);

        var result = new
        {
            promptId = rendered.Id.Name,
            version = rendered.Id.Version,
            contentSha256 = rendered.ContentSha256,
            packageVersion = rendered.PackageVersion,
            messages = rendered.Messages.Select(message => new { message.Role, message.Content }),
            hints = rendered.Hints
        };

        await _output.WriteLineAsync(JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));

        return 0;
    }

    private static IReadOnlyDictionary<string, object?> ReadArguments(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException($"Arguments file '{path}' must contain a JSON object.");
        }

        return document.RootElement.EnumerateObject().ToDictionary(
            property => property.Name,
            property => ToValue(property.Value),
            StringComparer.Ordinal);
    }

    private static object? ToValue(JsonElement value)
        => value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when value.TryGetInt64(out var integer) => integer,
            JsonValueKind.Number => value.GetDecimal(),
            JsonValueKind.Null => null,
            _ => value.GetRawText()
        };

    private static CommandOptions ParseOptions(string[] args, bool allowArguments)
    {
        string? root = null;
        string? argsFile = null;
        var arguments = new List<string>();

        for (var index = 0; index < args.Length; index++)
        {
            var option = args[index];
            if (!allowArguments && option == "--root" && index + 1 < args.Length)
            {
                root = args[++index];
            }
            else if (allowArguments && option == "--args" && index + 1 < args.Length)
            {
                argsFile = args[++index];
            }
            else if (allowArguments && option == "--arg" && index + 1 < args.Length)
            {
                arguments.Add(args[++index]);
            }
            else
            {
                throw new ArgumentException($"Unknown or incomplete option '{option}'.");
            }
        }

        return new CommandOptions(root, argsFile, arguments);
    }

    private Task<int> FailAsync(string message)
    {
        _error.WriteLine(message);
        return Task.FromResult(1);
    }

    private static bool IsHelp(string value) => value is "help" or "--help" or "-h";

    private sealed record CommandOptions(string? Root, string? ArgsFile, IReadOnlyList<string> Arguments);

    private const string Usage = """
        DevComputaria.Prompts.Cli

        Commands:
          validate [--root <repository>]  Validate schemas, manifest, aliases and includes.
          render <id>@<version> [--args <file.json>] [--arg key=value]

        Run '<command> --help' for command details.
        """;

    private const string RenderUsage = """
        Usage:
          render <id>@<version> [--args <file.json>] [--arg key=value]

        --args loads arguments from a JSON object. Repeat --arg to add or override values.
        """;

    private const string ValidateUsage = """
        Usage:
          validate [--root <repository>]

        Validates schemas, manifest/resources, aliases, identities, includes and output schemas.
        """;
}
