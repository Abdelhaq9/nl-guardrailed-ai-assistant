using System.Text.RegularExpressions;

namespace GuardrailedAiAssistant.Guardrails;

public static class Policy
{
    // Examples of hard deterministic boundaries.
    // In a real system, you'd expand these and route to proper services.
    public static string? TryHandleDeterministically(string input)
    {
        var s = input.Trim();

        // Security / secrets / auth: refuse deterministically (no LLM)
        if (ContainsSecretRequest(s))
            return "I can’t help with credential, secret, or hacking-related requests.";

        // Simple arithmetic: deterministic local compute (no LLM)
        var math = TryComputeMath(s);
        if (math is not null)
            return math;

        return null;
    }

    private static bool ContainsSecretRequest(string input)
    {
        var lower = input.ToLowerInvariant();
        return lower.Contains("password")
            || lower.Contains("api key")
            || lower.Contains("secret")
            || lower.Contains("token")
            || lower.Contains("hack")
            || lower.Contains("bypass")
            || lower.Contains("exploit");
    }

    // Tiny demo math: "calculate 17*19" or "17*19"
    private static string? TryComputeMath(string input)
    {
        var lower = input.ToLowerInvariant().Replace("calculate", "").Trim();

        // Accept digits + operators only
        if (!Regex.IsMatch(lower, @"^[0-9+\-*/().\s]+$"))
            return null;

        try
        {
            // VERY small safe evaluator for demo: only + - * / and integers/decimals
            // (Still not production-grade; production would use a real expression parser.)
            var result = SimpleMathEvaluator.Evaluate(lower);
            return $"Result: {result}";
        }
        catch
        {
            return "I couldn't evaluate that expression safely.";
        }
    }

    private static class SimpleMathEvaluator
    {
        public static decimal Evaluate(string expr)
        {
            // Shunting-yard is overkill here; keep it minimal:
            // Use DataTable is also not ideal. We'll implement a tiny parser.
            var parser = new ExpressionParser(expr);
            return parser.ParseExpression();
        }

        private sealed class ExpressionParser
        {
            private readonly string _s;
            private int _i;

            public ExpressionParser(string s) { _s = s; _i = 0; }

            public decimal ParseExpression()
            {
                var value = ParseTerm();
                while (true)
                {
                    SkipWs();
                    if (Match('+')) value += ParseTerm();
                    else if (Match('-')) value -= ParseTerm();
                    else break;
                }
                return value;
            }

            private decimal ParseTerm()
            {
                var value = ParseFactor();
                while (true)
                {
                    SkipWs();
                    if (Match('*')) value *= ParseFactor();
                    else if (Match('/'))
                    {
                        var denom = ParseFactor();
                        if (denom == 0) throw new DivideByZeroException();
                        value /= denom;
                    }
                    else break;
                }
                return value;
            }

            private decimal ParseFactor()
            {
                SkipWs();
                if (Match('('))
                {
                    var inner = ParseExpression();
                    SkipWs();
                    if (!Match(')')) throw new FormatException("Missing ')'");
                    return inner;
                }

                return ParseNumber();
            }

            private decimal ParseNumber()
            {
                SkipWs();
                int start = _i;
                while (_i < _s.Length && (char.IsDigit(_s[_i]) || _s[_i] == '.')) _i++;
                if (start == _i) throw new FormatException("Expected number");

                var token = _s[start.._i];
                if (!decimal.TryParse(token, out var n)) throw new FormatException("Bad number");
                return n;
            }

            private void SkipWs()
            {
                while (_i < _s.Length && char.IsWhiteSpace(_s[_i])) _i++;
            }

            private bool Match(char c)
            {
                if (_i < _s.Length && _s[_i] == c)
                {
                    _i++;
                    return true;
                }
                return false;
            }
        }
    }
}
