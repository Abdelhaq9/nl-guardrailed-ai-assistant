using System.Text;
using GuardrailedAiAssistant.Search;

namespace GuardrailedAiAssistant.Tools;

public sealed class RunbookSearchTool
{
    private readonly SemanticSearchEngine _engine;

    public RunbookSearchTool(SemanticSearchEngine engine)
    {
        _engine = engine;
    }

    public async Task<string> SearchAsync(string query, CancellationToken ct)
    {
        var results = await _engine.SearchAsync(query, topK: 3, ct);

        if (results.Count == 0)
            return "No relevant runbook documents found.";

        var sb = new StringBuilder();
        sb.AppendLine("Relevant documents:");
        foreach (var r in results)
        {
            sb.AppendLine($"- {r.Doc.Id}: {r.Doc.Title} (score: {r.Score:F3})");
            sb.AppendLine($"  Excerpt: {Truncate(r.Doc.Body, 180)}");
        }

        return sb.ToString();
    }

    private static string Truncate(string s, int max)
        => s.Length <= max ? s : s[..max] + "...";
}
