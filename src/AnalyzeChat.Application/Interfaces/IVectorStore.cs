using AnalyzeChat.Domain.Entities;

namespace AnalyzeChat.Application.Interfaces;

/// <summary>
/// Stores and searches text chunks (RAG).
/// </summary>
public interface IVectorStore
{
    /// <summary>
    /// Stores a collection of text chunks for a document.
    /// </summary>
    Task StoreChunksAsync(Guid documentId, IEnumerable<TextChunk> chunks);

    /// <summary>
    /// Searches for the most relevant chunks matching the query embedding.
    /// </summary>
    Task<List<TextChunk>> SearchAsync(float[] queryEmbedding, Guid documentId, int topK = 5);

    /// <summary>
    /// Keyword-based search for relevant chunks.
    /// </summary>
    Task<List<TextChunk>> SearchByKeywordAsync(string query, Guid documentId, int topK = 5);

    /// <summary>
    /// Deletes all chunks for a document.
    /// </summary>
    Task DeleteDocumentChunksAsync(Guid documentId);
}
