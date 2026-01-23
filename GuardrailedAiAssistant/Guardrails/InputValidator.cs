namespace GuardrailedAiAssistant.Guardrails;

public static class InputValidator
{
    public static ValidationResult ValidateUserInput(string input, int maxChars)
    {
        if (string.IsNullOrWhiteSpace(input))
            return ValidationResult.Fail("Please enter a question.");

        var trimmed = input.Trim();

        if (trimmed.Length > maxChars)
            return ValidationResult.Fail($"Input too long. Max allowed is {maxChars} characters.");

        // Basic “don’t even attempt” patterns (demo-grade)
        var lower = trimmed.ToLowerInvariant();

        // Very rough prompt-injection / exfil indicators
        if (lower.Contains("ignore previous instructions") ||
            lower.Contains("reveal system prompt") ||
            lower.Contains("print the hidden") ||
            lower.Contains("developer message"))
        {
            return ValidationResult.Fail("I can’t process that request.");
        }

        return ValidationResult.Success();
    }
}

public readonly record struct ValidationResult(bool Ok, string? ErrorMessage)
{
    public static ValidationResult Success() => new(true, null);
    public static ValidationResult Fail(string message) => new(false, message);
}
