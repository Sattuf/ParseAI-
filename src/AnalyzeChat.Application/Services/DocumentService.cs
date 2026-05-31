using AnalyzeChat.Application.DTOs;
using AnalyzeChat.Application.Interfaces;
using AnalyzeChat.Domain.Entities;
using AnalyzeChat.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace AnalyzeChat.Application.Services;

/// <summary>
/// Handles document upload, PDF parsing, chunking, and storage.
/// </summary>
public class DocumentService
{
    private readonly IPdfService _pdfService;
    private readonly IChunkingService _chunkingService;
    private readonly IVectorStore _vectorStore;
    private readonly IChatAIService _chatAIService;
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        IPdfService pdfService,
        IChunkingService chunkingService,
        IVectorStore vectorStore,
        IChatAIService chatAIService,
        IApplicationDbContext dbContext,
        ILogger<DocumentService> logger)
    {
        _pdfService = pdfService;
        _chunkingService = chunkingService;
        _vectorStore = vectorStore;
        _chatAIService = chatAIService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<DocumentDto> UploadAndProcessAsync(string fileName, long fileSize, Stream pdfStream)
    {
        var document = new Document
        {
            FileName = fileName,
            FileSizeBytes = fileSize,
            Status = DocumentStatus.Processing,
        };

        _dbContext.Documents.Add(document);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Processing document {DocId}: {FileName}", document.Id, fileName);

        try
        {
            // 1. Extract text from PDF
            var pages = await _pdfService.ExtractTextAsync(pdfStream);
            document.PageCount = pages.Count;
            _logger.LogInformation("Extracted {PageCount} pages from {FileName}", pages.Count, fileName);

            // 2. Smart chunking
            var chunks = _chunkingService.ChunkText(document.Id, pages);
            _logger.LogInformation("Created {ChunkCount} chunks from {FileName}", chunks.Count, fileName);

            // 3. Skip Embeddings Generation (API key does not support text-embedding-004)
            // _logger.LogInformation("Generating embeddings for {ChunkCount} chunks...", chunks.Count);
            // foreach (var chunk in chunks)
            // {
            //     chunk.Embedding = await _chatAIService.GenerateEmbeddingAsync(chunk.Content); 
            // }

            // 4. Store chunks
            await _vectorStore.StoreChunksAsync(document.Id, chunks);
            _logger.LogInformation("Stored {ChunkCount} chunks for {FileName}", chunks.Count, fileName);

            document.Status = DocumentStatus.Ready;
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process document {DocId}", document.Id);
            document.Status = DocumentStatus.Failed;
            document.ErrorMessage = ex.Message;
            await _dbContext.SaveChangesAsync();
        }

        return ToDto(document);
    }

    public async Task<DocumentDto?> GetDocumentAsync(Guid documentId)
    {
        var doc = await _dbContext.Documents.FindAsync(documentId);
        return doc != null ? ToDto(doc) : null;
    }

    private static DocumentDto ToDto(Document doc) => new()
    {
        Id = doc.Id,
        FileName = doc.FileName,
        FileSizeBytes = doc.FileSizeBytes,
        PageCount = doc.PageCount,
        Status = doc.Status,
        UploadedAt = doc.UploadedAt,
        ErrorMessage = doc.ErrorMessage,
    };
}
