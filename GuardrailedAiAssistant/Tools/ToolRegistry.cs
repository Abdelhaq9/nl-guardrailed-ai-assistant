using System.Text.Json;
using GuardrailedAiAssistant.Search;

namespace GuardrailedAiAssistant.Tools;

public sealed class ToolRegistry
{
    private readonly AppConfig _config;
    private readonly WorldTimeTool _time;
    private readonly RunbookSearchTool _runbooks;

    // Allowlist: only these tools can run
    private static readonly HashSet<string> AllowedTools = new(StringComparer.OrdinalIgnoreCase)
    {
        "WorldTime.GetCityTime",
        "Runbooks.Search"
    };

    public ToolRegistry(AppConfig config, SemanticSearchEngine searchEngine)
    {
        _config = config;
        _time = new WorldTimeTool();
        _runbooks = new RunbookSearchTool(searchEngine);
    }

    public async Task<ToolExecutionResult> TryExecuteAsync(GuardrailedAiAssistant.Llm.ToolPlan plan)
    {
        if (plan.Action != GuardrailedAiAssistant.Llm.ToolPlanAction.Tool)
            return ToolExecutionResult.Fail("Tool execution requested incorrectly.");

        if (string.IsNullOrWhiteSpace(plan.ToolName))
            return ToolExecutionResult.Fail("Missing tool name.");

        if (!AllowedTools.Contains(plan.ToolName))
            return ToolExecutionResult.Fail("Tool not allowed.");

        // Timeout guardrail
        using var cts = new CancellationTokenSource(_config.ToolTimeout);

        try
        {
            return plan.ToolName switch
            {
                "WorldTime.GetCityTime" => await ExecWorldTimeAsync(plan.Arguments, cts.Token),
                "Runbooks.Search" => await ExecRunbookSearchAsync(plan.Arguments, cts.Token),
                _ => ToolExecutionResult.Fail("Tool not allowed.")
            };
        }
        catch (OperationCanceledException)
        {
            return ToolExecutionResult.Fail("Tool timed out. Try again with a simpler request.");
        }
        catch
        {
            return ToolExecutionResult.Fail("Tool failed safely. Try again.");
        }
    }

    private Task<ToolExecutionResult> ExecWorldTimeAsync(JsonElement? args, CancellationToken ct)
    {
        // Arg validation guardrail
        if (args is null || args.Value.ValueKind != JsonValueKind.Object)
            return Task.FromResult(ToolExecutionResult.Fail("Invalid tool arguments."));

        if (!args.Value.TryGetProperty("city", out var cityProp))
            return Task.FromResult(ToolExecutionResult.Fail("Missing required argument: city"));

        var city = cityProp.GetString();
        if (string.IsNullOrWhiteSpace(city) || city.Length > 64)
            return Task.FromResult(ToolExecutionResult.Fail("Invalid city."));

        // Deterministic tool
        var output = _time.GetCityTime(city);
        return Task.FromResult(ToolExecutionResult.Success(output));
    }

    private async Task<ToolExecutionResult> ExecRunbookSearchAsync(JsonElement? args, CancellationToken ct)
    {
        if (args is null || args.Value.ValueKind != JsonValueKind.Object)
            return ToolExecutionResult.Fail("Invalid tool arguments.");

        if (!args.Value.TryGetProperty("query", out var queryProp))
            return ToolExecutionResult.Fail("Missing required argument: query");

        var q = queryProp.GetString();
        if (string.IsNullOrWhiteSpace(q) || q.Length > 500)
            return ToolExecutionResult.Fail("Invalid query.");

        var output = await _runbooks.SearchAsync(q, ct);
        return ToolExecutionResult.Success(output);
    }
}

public sealed record ToolExecutionResult(bool Ok, string? Output, string SafeMessage)
{
    public static ToolExecutionResult Success(string output) => new(true, output, "");
    public static ToolExecutionResult Fail(string safeMessage) => new(false, null, safeMessage);
}
