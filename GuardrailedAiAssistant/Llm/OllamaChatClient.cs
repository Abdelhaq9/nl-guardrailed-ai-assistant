using System.Text;
using System.Text.Json;

namespace GuardrailedAiAssistant.Llm;

public sealed class OllamaChatClient
{
    private readonly HttpClient _http = new();
    private readonly Uri _baseUri;
    private readonly string _model;

    public OllamaChatClient(Uri baseUri, string model)
    {
        _baseUri = baseUri;
        _model = model;
    }

    public async Task<string> ChatAsync(IEnumerable<ChatMessage> messages, CancellationToken ct = default)
    {
        var payload = new
        {
            model = _model,
            stream = false,
            messages = messages.Select(m => new
            {
                role = m.Role,
                content = m.Content
            }).ToArray()
        };

        var json = JsonSerializer.Serialize(payload);

        using var resp = await _http.PostAsync(
            new Uri(_baseUri, "/api/chat"),
            new StringContent(json, Encoding.UTF8, "application/json"),
            ct);

        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new Exception($"Ollama /api/chat error: {text}");

        using var doc = JsonDocument.Parse(text);
        return doc.RootElement.GetProperty("message").GetProperty("content").GetString() ?? "";
    }
}

public sealed record ChatMessage(string Role, string Content);
