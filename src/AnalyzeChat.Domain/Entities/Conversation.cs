using System;
using System.Collections.Generic;

namespace AnalyzeChat.Domain.Entities;

public class Conversation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Foreign Keys
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid DocumentId { get; set; }
    public Document? Document { get; set; }

    // Navigation
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
