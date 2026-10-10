using System.Text.Json;
using DevComputaria.PromptKit.Abstractions;
using DevComputaria.PromptKit.Observability;
using Xunit;

namespace DevComputaria.PromptKit.Tests;

public sealed class PromptLogRedactorTests
{
    [Fact]
    public void RedactArguments_ShouldMaskSensitiveVariablesWithoutReadingTheirValue()
    {
        const string secret = "RG-123456-SENSITIVE";
        var prompt = CreatePrompt();
        var args = new PromptArgs(new Dictionary<string, object?>
        {
            ["country"] = "BR",
            ["ocr_text"] = new SecretValue(secret)
        });

        var safe = new PromptLogRedactor().RedactArguments(prompt, args);
        var serialized = JsonSerializer.Serialize(safe);

        Assert.Equal("BR", safe["country"]);
        Assert.Equal(PromptLogRedactor.RedactedValue, safe["ocr_text"]);
        Assert.DoesNotContain(secret, serialized, StringComparison.Ordinal);
        Assert.Equal(0, SecretValue.ToStringCalls);
    }

    [Fact]
    public void RedactArguments_ShouldOmitUndeclaredArgumentsAndMissingVariables()
    {
        var prompt = CreatePrompt();
        var args = new PromptArgs(new Dictionary<string, object?>
        {
            ["country"] = "BR",
            ["unexpected_token"] = "must-not-be-logged"
        });

        var safe = new PromptLogRedactor().RedactArguments(prompt, args);

        Assert.Single(safe);
        Assert.Equal("BR", safe["country"]);
        Assert.False(safe.ContainsKey("unexpected_token"));
        Assert.False(safe.ContainsKey("ocr_text"));
    }

    private static PromptSpec CreatePrompt()
        => new(
            new PromptId("image-analysis.analyze-document", "1.1.0"),
            new[] { new RenderedMessage("user", "{{ocr_text}}") },
            new[]
            {
                new PromptVariableSpec("country", isRequired: true, type: "string"),
                new PromptVariableSpec("ocr_text", isRequired: true, redactedInLogs: true, type: "string")
            });

    private sealed class SecretValue
    {
        public SecretValue(string value)
        {
            Value = value;
            ToStringCalls = 0;
        }

        public static int ToStringCalls { get; private set; }

        private string Value { get; }

        public override string ToString()
        {
            ToStringCalls++;
            return Value;
        }
    }
}
