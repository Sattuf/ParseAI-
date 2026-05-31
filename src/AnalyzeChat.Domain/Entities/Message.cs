using System;
using AnalyzeChat.Domain.Enums;

namespace AnalyzeChat.Domain.Entities;

public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public MessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Foreign Key
    public Guid ConversationId { get; set; }
    public Conversation? Conversation { get; set; }
}
