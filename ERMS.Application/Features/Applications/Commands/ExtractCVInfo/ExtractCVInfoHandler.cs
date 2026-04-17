using ERMS.Application.Interface;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.Applications.Commands.ExtractCVInfo;

public sealed class ExtractCVInfoHandler : IRequestHandler<ExtractCVInfoCommand, ExtractCVInfoResult>
{
    private readonly ICloudinaryService _cloudinaryService;
    private readonly IPdfTextExtractor _pdfTextExtractor;
    private readonly ICVParsingService _cvParsingService;
    private readonly ILogger<ExtractCVInfoHandler> _logger;

    public ExtractCVInfoHandler(
        ICloudinaryService cloudinaryService,
        IPdfTextExtractor pdfTextExtractor,
        ICVParsingService cvParsingService,
        ILogger<ExtractCVInfoHandler> logger)
    {
        _cloudinaryService = cloudinaryService;
        _pdfTextExtractor = pdfTextExtractor;
        _cvParsingService = cvParsingService;
        _logger = logger;
    }

    public async Task<ExtractCVInfoResult> Handle(ExtractCVInfoCommand request, CancellationToken cancellationToken)
    {
        string resumeUrl;
        string resumePublicId;
        await using (var stream = request.CvFile.OpenReadStream())
        {
            (resumeUrl, resumePublicId) = await _cloudinaryService.UploadPdfAsync(stream, request.CvFile.FileName);
        }

        string resumeText;
        await using (var stream = request.CvFile.OpenReadStream())
        {
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;
            resumeText = await _pdfTextExtractor.ExtractTextAsync(memoryStream);
        }

        var parsed = await _cvParsingService.ParseCVAsync(resumeText, cancellationToken);

        _logger.LogInformation("Trích xuất thông tin CV từ file đã tải lên {FileName}", request.CvFile.FileName);

        return new ExtractCVInfoResult
        {
            ResumeUrl = resumeUrl,
            ResumePublicId = resumePublicId,
            FullName = parsed.FullName,
            Email = parsed.Email,
            Phone = parsed.PhoneNumber,
            ResumeText = resumeText
        };
    }
}
