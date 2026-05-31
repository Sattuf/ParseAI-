namespace AnalyzeChat.Application.DTOs;

public class ChatRequest
{
    public Guid? ConversationId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string DocumentId { get; set; } = string.Empty;
    public string SelectedModel { get; set; } = "gemini-2.5-flash";
    public List<ChatHistoryItem> History { get; set; } = [];
}

public class ChatHistoryItem
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
