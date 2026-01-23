using System.Text;
using System.Text.Json;

namespace GuardrailedAiAssistant.Llm;

public sealed class OllamaEmbeddingGenerator : IDisposable
{
    private readonly HttpClient _http = new();
    private readonly Uri _baseUri;
    private readonly string _model;
    private bool _disposed;

    public OllamaEmbeddingGenerator(Uri baseUri, string model)
    {
        _baseUri = baseUri;
        _model = model;
    }

    public async Task<float[]> EmbedAsync(string input, CancellationToken ct = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(OllamaEmbeddingGenerator));

        var payload = new { model = _model, prompt = input };
        var json = JsonSerializer.Serialize(payload);

        using var resp = await _http.PostAsync(
            new Uri(_baseUri, "/api/embeddings"),
            new StringContent(json, Encoding.UTF8, "application/json"),
            ct);

        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new Exception($"Ollama /api/embeddings error: {text}");

        using var doc = JsonDocument.Parse(text);
        return doc.RootElement.GetProperty("embedding").EnumerateArray().Select(x => x.GetSingle()).ToArray();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _http.Dispose();
        _disposed = true;
    }
}
