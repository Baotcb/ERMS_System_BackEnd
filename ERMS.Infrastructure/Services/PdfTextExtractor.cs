using ERMS.Application.Interface;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using System.Text;

namespace ERMS.Infrastructure.Services;

/// <summary>
/// PDF text extraction using UglyToad.PdfPig (MIT License)
/// </summary>
public class PdfTextExtractor : IPdfTextExtractor
{
    private readonly ILogger<PdfTextExtractor> _logger;

    public PdfTextExtractor(ILogger<PdfTextExtractor> logger)
    {
        _logger = logger;
    }

    public Task<string> ExtractTextAsync(Stream pdfStream)
    {
        try
        {
            // PdfPig requires a seekable stream
            // Always copy to MemoryStream to avoid unsafe casts and ensure stream is seekable
            using var memoryStream = new MemoryStream();
            
            // Reset source stream position if possible
            if (pdfStream.CanSeek)
            {
                pdfStream.Position = 0;
            }
            
            pdfStream.CopyTo(memoryStream);
            memoryStream.Position = 0;

            var textBuilder = new StringBuilder();

            using var document = PdfDocument.Open(memoryStream);

            _logger.LogInformation("Extracting text from PDF with {PageCount} pages", document.NumberOfPages);

            foreach (var page in document.GetPages())
            {
                var pageText = page.Text;
                textBuilder.AppendLine(pageText);
            }

            var extractedText = textBuilder.ToString().Trim();

            _logger.LogInformation("Extracted {CharCount} characters from PDF", extractedText.Length);

            return Task.FromResult(extractedText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract text from PDF");
            throw new Exception("Failed to extract text from PDF. Please ensure the file is a valid PDF document.", ex);
        }
    }
}
