using System.Text.RegularExpressions;
using AnalyzeChat.Application.Interfaces;
using AnalyzeChat.Domain.Entities;

namespace AnalyzeChat.Infrastructure.Services;

/// <summary>
/// Splits text into semantic chunks by paragraphs/headings, 
/// preserving meaning with overlap between chunks.
/// </summary>
public class SmartChunkingService : IChunkingService
{
    private const int MaxChunkSize = 500;   // ~500 tokens (words approximation)
    private const int OverlapSize = 50;     // Overlap between chunks

    public List<TextChunk> ChunkText(Guid documentId, List<(int PageNumber, string Text)> pages)
    {
        var chunks = new List<TextChunk>();
        int chunkIndex = 0;

        foreach (var (pageNumber, text) in pages)
        {
            // Split by paragraphs (double newlines, or heading patterns)
            var paragraphs = SplitIntoParagraphs(text);
            var currentChunk = "";

            foreach (var paragraph in paragraphs)
            {
                var trimmed = paragraph.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;

                // If adding this paragraph exceeds max, save current chunk and start new
                var wordCount = CountWords(currentChunk + " " + trimmed);
                if (wordCount > MaxChunkSize && !string.IsNullOrEmpty(currentChunk))
                {
                    chunks.Add(new TextChunk
                    {
                        DocumentId = documentId,
                        ChunkIndex = chunkIndex++,
                        Content = currentChunk.Trim(),
                        PageNumber = pageNumber,
                    });

                    // Keep overlap from end of previous chunk
                    var words = currentChunk.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    currentChunk = words.Length > OverlapSize
                        ? string.Join(' ', words.TakeLast(OverlapSize)) + " " + trimmed
                        : trimmed;
                }
                else
                {
                    currentChunk = string.IsNullOrEmpty(currentChunk)
                        ? trimmed
                        : currentChunk + "\n" + trimmed;
                }
            }

            // Don't forget the last chunk from this page
            if (!string.IsNullOrWhiteSpace(currentChunk))
            {
                chunks.Add(new TextChunk
                {
                    DocumentId = documentId,
                    ChunkIndex = chunkIndex++,
                    Content = currentChunk.Trim(),
                    PageNumber = pageNumber,
                });
            }
        }

        return chunks;
    }

    private static List<string> SplitIntoParagraphs(string text)
    {
        // Split on double newlines, numbered headings, or bullet points
        return Regex.Split(text, @"\n{2,}|(?=\n\s*(?:\d+[\.\)]\s|[-•]\s|#{1,3}\s))")
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();
    }

    private static int CountWords(string text)
    {
        return text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
    }
}
