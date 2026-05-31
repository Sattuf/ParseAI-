using AnalyzeChat.Application.Interfaces;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace AnalyzeChat.Infrastructure.Services;

/// <summary>
/// Extracts text from PDF files using PdfPig library.
/// </summary>
public class PdfParserService : IPdfService
{
    private readonly ILogger<PdfParserService> _logger;

    public PdfParserService(ILogger<PdfParserService> logger)
    {
        _logger = logger;
    }

    public Task<List<(int PageNumber, string Text)>> ExtractTextAsync(Stream pdfStream)
    {
        var results = new List<(int PageNumber, string Text)>();

        try
        {
            using var document = PdfDocument.Open(pdfStream);

            foreach (var page in document.GetPages())
            {
                var text = page.Text;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    results.Add((page.Number, text.Trim()));
                }
            }

            _logger.LogInformation("Extracted text from {PageCount} pages", results.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract text from PDF");
            throw;
        }

        return Task.FromResult(results);
    }
}
