namespace AnalyzeChat.Domain.Entities;

/// <summary>
/// Represents a chunk of text extracted from a PDF document,
/// stored with its embedding vector for semantic search.
/// </summary>
public class TextChunk
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public int ChunkIndex { get; set; }
    public string Content { get; set; } = string.Empty;
    public int PageNumber { get; set; }
    public float[]? Embedding { get; set; }
}
