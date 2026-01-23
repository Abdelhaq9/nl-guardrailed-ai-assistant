using GuardrailedAiAssistant.Llm;

namespace GuardrailedAiAssistant.Search;

public sealed class SemanticSearchEngine
{
    private readonly List<(KnowledgeDocument Doc, float[] Vec)> _index;
    private readonly OllamaEmbeddingGenerator _embeddings;

    private SemanticSearchEngine(List<(KnowledgeDocument, float[])> index, OllamaEmbeddingGenerator embeddings)
    {
        _index = index;
        _embeddings = embeddings;
    }

    public static async Task<SemanticSearchEngine> BuildAsync(
        IEnumerable<KnowledgeDocument> docs,
        OllamaEmbeddingGenerator embeddings,
        CancellationToken ct = default)
    {
        var index = new List<(KnowledgeDocument, float[])>();

        foreach (var d in docs)
        {
            var vec = await embeddings.EmbedAsync(d.Body, ct);
            index.Add((d, vec));
        }

        return new SemanticSearchEngine(index, embeddings);
    }

    public async Task<List<(KnowledgeDocument Doc, float Score)>> SearchAsync(
        string query,
        int topK,
        CancellationToken ct = default)
    {
        var q = await _embeddings.EmbedAsync(query, ct);

        return _index
            .Select(e => (e.Doc, Score: CosineSimilarity(e.Vec, q)))
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .ToList();
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length) return 0;

        double dot = 0;
        double normA = 0;
        double normB = 0;

        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        var denom = Math.Sqrt(normA) * Math.Sqrt(normB);
        if (denom == 0) return 0;
        return (float)(dot / denom);
    }
}
