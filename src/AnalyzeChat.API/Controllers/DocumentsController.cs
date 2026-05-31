using AnalyzeChat.Application.DTOs;
using AnalyzeChat.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnalyzeChat.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly DocumentService _documentService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(DocumentService documentService, ILogger<DocumentsController> logger)
    {
        _documentService = documentService;
        _logger = logger;
    }

    /// <summary>
    /// Upload a PDF document for processing.
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(50_000_000)] // 50MB limit
    public async Task<ActionResult<DocumentDto>> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded" });

        if (!file.ContentType.Contains("pdf", StringComparison.OrdinalIgnoreCase)
            && !file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Only PDF files are accepted" });

        _logger.LogInformation("Uploading file: {FileName} ({Size} bytes)", file.FileName, file.Length);

        using var stream = file.OpenReadStream();
        var result = await _documentService.UploadAndProcessAsync(file.FileName, file.Length, stream);

        return Ok(result);
    }

    /// <summary>
    /// Get document info by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DocumentDto>> GetDocument(Guid id)
    {
        var doc = await _documentService.GetDocumentAsync(id);
        if (doc == null)
            return NotFound(new { error = "Document not found" });

        return Ok(doc);
    }
}
