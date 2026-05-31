namespace AnalyzeChat.Domain.Entities;

public class ChatSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Document? Document { get; set; }
    public List<ChatMessage> Messages { get; set; } = [];
}
