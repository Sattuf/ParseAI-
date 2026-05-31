using AnalyzeChat.Domain.Entities;

namespace AnalyzeChat.Application.Interfaces;

/// <summary>
/// Splits extracted PDF text into meaningful semantic chunks.
/// </summary>
public interface IChunkingService
{
    /// <summary>
    /// Splits page texts into semantic chunks preserving context.
    /// </summary>
    List<TextChunk> ChunkText(Guid documentId, List<(int PageNumber, string Text)> pages);
}
