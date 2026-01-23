namespace GuardrailedAiAssistant.Search;

public sealed class KnowledgeDocument
{
    public string Id { get; }
    public string Title { get; }
    public string Body { get; }

    public KnowledgeDocument(string id, string title, string body)
    {
        Id = id;
        Title = title;
        Body = body;
    }

    public override string ToString() => Title;
}
