using AnalyzeChat.Domain.Enums;

namespace AnalyzeChat.Domain.Entities;

public class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public int PageCount { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Uploading;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string? ErrorMessage { get; set; }

    // Navigation
    public List<Conversation> Conversations { get; set; } = [];
}
