namespace AnalyzeChat.Application.Interfaces;

/// <summary>
/// Extracts text from PDF files.
/// </summary>
public interface IPdfService
{
    /// <summary>
    /// Extracts text content from a PDF stream, returning text per page.
    /// </summary>
    Task<List<(int PageNumber, string Text)>> ExtractTextAsync(Stream pdfStream);
}
