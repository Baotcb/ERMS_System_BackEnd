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

            _logger.LogInformation("Đang trích xuất văn bản từ PDF với {PageCount} trang", document.NumberOfPages);

            foreach (var page in document.GetPages())
            {
                var pageText = page.Text;
                textBuilder.AppendLine(pageText);
            }

            var extractedText = textBuilder.ToString().Trim();

            _logger.LogInformation("Đã trích xuất {CharCount} ký tự từ PDF", extractedText.Length);

            return Task.FromResult(extractedText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không thể trích xuất văn bản từ PDF");
            throw new Exception("Không thể trích xuất văn bản từ PDF. Vui lòng đảm bảo file là tài liệu PDF hợp lệ.", ex);
        }
    }
}
