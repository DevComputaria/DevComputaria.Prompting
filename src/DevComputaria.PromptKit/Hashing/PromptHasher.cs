using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using DevComputaria.PromptKit.Abstractions;

namespace DevComputaria.PromptKit.Hashing;

public sealed class PromptHasher
{
    public string ComputeHash(PromptSpec prompt, PromptArgs args, IEnumerable<RenderedMessage> renderedMessages)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(renderedMessages);

        var canonical = BuildCanonicalPayload(prompt, args, renderedMessages);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string BuildCanonicalPayload(PromptSpec prompt, PromptArgs args, IEnumerable<RenderedMessage> renderedMessages)
    {
        var builder = new StringBuilder();

        builder.AppendLine("prompt.id=" + prompt.Id.Name);
        builder.AppendLine("prompt.version=" + prompt.Id.Version);
        builder.AppendLine("prompt.outputSchemaRef=" + (prompt.OutputSchemaRef ?? string.Empty));

        builder.AppendLine("prompt.includes.count=" + prompt.Includes.Count.ToString(CultureInfo.InvariantCulture));
        foreach (var include in prompt.Includes.OrderBy(x => x, StringComparer.Ordinal))
        {
            builder.AppendLine("prompt.include=" + include);
        }

        builder.AppendLine("prompt.variables.count=" + prompt.Variables.Count.ToString(CultureInfo.InvariantCulture));
        foreach (var variable in prompt.Variables.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            builder.AppendLine($"prompt.variable={variable.Key}|required={variable.Value.IsRequired}|redacted={variable.Value.RedactedInLogs}|type={variable.Value.Type ?? string.Empty}");
        }

        var parts = prompt.Parts.ToArray();
        builder.AppendLine("prompt.parts.count=" + parts.Length.ToString(CultureInfo.InvariantCulture));
        for (var i = 0; i < parts.Length; i++)
        {
            builder.AppendLine($"prompt.part[{i}].role={parts[i].Role}");
            builder.AppendLine($"prompt.part[{i}].content={Escape(parts[i].Content)}");
        }

        var orderedArgs = args.OrderBy(x => x.Key, StringComparer.Ordinal).ToArray();
        builder.AppendLine("prompt.args.count=" + orderedArgs.Length.ToString(CultureInfo.InvariantCulture));
        foreach (var (key, value) in orderedArgs)
        {
            builder.AppendLine($"prompt.arg.{key}={SerializeValue(value)}");
        }

        var rendered = renderedMessages.ToArray();
        builder.AppendLine("rendered.messages.count=" + rendered.Length.ToString(CultureInfo.InvariantCulture));
        for (var i = 0; i < rendered.Length; i++)
        {
            builder.AppendLine($"rendered.message[{i}].role={rendered[i].Role}");
            builder.AppendLine($"rendered.message[{i}].content={Escape(rendered[i].Content)}");
        }

        return builder.ToString();
    }

    private static string SerializeValue(object? value)
    {
        return value switch
        {
            null => "<null>",
            string text => "string:" + Escape(text),
            bool boolean => "bool:" + (boolean ? "true" : "false"),
            byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal
                => "number:" + Convert.ToString(value, CultureInfo.InvariantCulture),
            DateTime dateTime => "datetime:" + dateTime.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffset => "datetimeoffset:" + dateTimeOffset.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            Guid guid => "guid:" + guid.ToString("D"),
            Enum @enum => "enum:" + @enum.GetType().FullName + ":" + Convert.ToString(@enum, CultureInfo.InvariantCulture),
            _ => "object:" + Escape(value.ToString() ?? string.Empty)
        };
    }

    private static string Escape(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
    }
}