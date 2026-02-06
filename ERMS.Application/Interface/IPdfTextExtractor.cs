namespace ERMS.Application.Interface;

/// <summary>
/// Interface for PDF text extraction
/// </summary>
public interface IPdfTextExtractor
{
    /// <summary>
    /// Extracts all text content from a PDF file
    /// </summary>
    /// <param name="pdfStream">The PDF file stream</param>
    /// <returns>Extracted plain text</returns>
    Task<string> ExtractTextAsync(Stream pdfStream);
}
