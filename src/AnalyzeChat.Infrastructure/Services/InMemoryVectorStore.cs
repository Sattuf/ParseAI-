using AnalyzeChat.Application.Interfaces;
using AnalyzeChat.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AnalyzeChat.Infrastructure.Services;

/// <summary>
/// In-memory store with keyword-based search (TF-IDF scoring).
/// No embeddings or external services needed.
/// </summary>
public class InMemoryVectorStore : IVectorStore
{
    private readonly ILogger<InMemoryVectorStore> _logger;
    private readonly Dictionary<Guid, List<TextChunk>> _store = new();

    public InMemoryVectorStore(ILogger<InMemoryVectorStore> logger)
    {
        _logger = logger;
    }

    public Task StoreChunksAsync(Guid documentId, IEnumerable<TextChunk> chunks)
    {
        var chunkList = chunks.ToList();
        _store[documentId] = chunkList;
        _logger.LogInformation("Stored {Count} chunks in-memory for document {DocId}", chunkList.Count, documentId);
        return Task.CompletedTask;
    }

    public Task<List<TextChunk>> SearchAsync(float[] queryEmbedding, Guid documentId, int topK = 5)
    {
        if (!_store.TryGetValue(documentId, out var chunks))
        {
            _logger.LogWarning("No chunks found for document {DocId}", documentId);
            return Task.FromResult(new List<TextChunk>());
        }

        if (queryEmbedding == null || queryEmbedding.Length == 0)
        {
            return Task.FromResult(chunks.Take(topK).ToList());
        }

        var scored = chunks
            .Where(c => c.Embedding != null && c.Embedding.Length == queryEmbedding.Length)
            .Select(c => new
            {
                Chunk = c,
                Score = CosineSimilarity(queryEmbedding, c.Embedding!)
            })
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .Select(x => x.Chunk)
            .ToList();

        _logger.LogInformation("Semantic search found {Count} chunks using vector embeddings", scored.Count);
        return Task.FromResult(scored.Count > 0 ? scored : chunks.Take(topK).ToList());
    }

    private static float CosineSimilarity(float[] vector1, float[] vector2)
    {
        if (vector1.Length != vector2.Length) return 0;
        
        float dotProduct = 0;
        float magnitude1 = 0;
        float magnitude2 = 0;

        for (int i = 0; i < vector1.Length; i++)
        {
            dotProduct += vector1[i] * vector2[i];
            magnitude1 += vector1[i] * vector1[i];
            magnitude2 += vector2[i] * vector2[i];
        }

        magnitude1 = (float)Math.Sqrt(magnitude1);
        magnitude2 = (float)Math.Sqrt(magnitude2);

        if (magnitude1 == 0 || magnitude2 == 0) return 0;

        return dotProduct / (magnitude1 * magnitude2);
    }

    /// <summary>
    /// Keyword-based search using TF-IDF-like scoring.
    /// </summary>
    public Task<List<TextChunk>> SearchByKeywordAsync(string query, Guid documentId, int topK = 5)
    {
        if (!_store.TryGetValue(documentId, out var chunks))
        {
            _logger.LogWarning("No chunks found for document {DocId}", documentId);
            return Task.FromResult(new List<TextChunk>());
        }

        // Tokenize the query
        var queryTerms = Tokenize(query);
        if (queryTerms.Count == 0)
        {
            return Task.FromResult(chunks.Take(topK).ToList());
        }

        // Score each chunk by keyword overlap
        var scored = chunks
            .Select(c =>
            {
                var chunkTerms = Tokenize(c.Content);
                var score = 0.0;

                foreach (var term in queryTerms)
                {
                    var termFreq = chunkTerms.Count(t => t.Contains(term, StringComparison.OrdinalIgnoreCase));
                    if (termFreq > 0)
                    {
                        // TF * IDF approximation
                        var tf = (double)termFreq / chunkTerms.Count;
                        var chunksWithTerm = chunks.Count(ch => 
                            ch.Content.Contains(term, StringComparison.OrdinalIgnoreCase));
                        var idf = Math.Log((double)chunks.Count / (1 + chunksWithTerm));
                        score += tf * idf;
                    }
                }

                return new { Chunk = c, Score = score };
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .Select(x => x.Chunk)
            .ToList();

        _logger.LogInformation("Keyword search found {Count} relevant chunks for query: {Query}", 
            scored.Count, query.Length > 50 ? query[..50] + "..." : query);

        // If no keyword matches, return first chunks as fallback
        return Task.FromResult(scored.Count > 0 ? scored : chunks.Take(topK).ToList());
    }

    public Task DeleteDocumentChunksAsync(Guid documentId)
    {
        _store.Remove(documentId);
        return Task.CompletedTask;
    }

    private static List<string> Tokenize(string text)
    {
        // Simple tokenization: split on whitespace and punctuation, filter short tokens
        return text
            .Split([' ', '\n', '\r', '\t', '.', ',', '!', '?', ':', ';', '(', ')', '[', ']', '{', '}', '/', '\\', '-', '=', '+', '*', '"', '\''],
                StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length >= 2)
            .Select(t => t.ToLowerInvariant())
            .ToList();
    }
}
