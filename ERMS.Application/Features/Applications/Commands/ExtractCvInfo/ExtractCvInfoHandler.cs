using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.Applications.Commands.ExtractCvInfo;

public sealed class ExtractCvInfoHandler : IRequestHandler<ExtractCvInfoCommand, ExtractCvInfoResult>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly IPdfTextExtractor _pdfTextExtractor;
    private readonly ICvInfoExtractorService _cvInfoExtractor;
    private readonly ILogger<ExtractCvInfoHandler> _logger;

    public ExtractCvInfoHandler(
        ICurrentUserService currentUserService,
        ICloudinaryService cloudinaryService,
        IPdfTextExtractor pdfTextExtractor,
        ICvInfoExtractorService cvInfoExtractor,
        ILogger<ExtractCvInfoHandler> logger)
    {
        _currentUserService = currentUserService;
        _cloudinaryService = cloudinaryService;
        _pdfTextExtractor = pdfTextExtractor;
        _cvInfoExtractor = cvInfoExtractor;
        _logger = logger;
    }

    public async Task<ExtractCvInfoResult> Handle(ExtractCvInfoCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate HRManager role
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

        if (!_currentUserService.Roles.Contains(AppRoles.HRManager))
            throw new UnauthorizedAccessException("Chỉ HR Manager mới có quyền thêm CV.");

        // 2. Upload CV to Cloudinary
        string resumeUrl, resumePublicId;
        await using (var stream = request.CvFile.OpenReadStream())
        {
            (resumeUrl, resumePublicId) = await _cloudinaryService.UploadPdfAsync(stream, request.CvFile.FileName);
        }

        // 3. Extract text from PDF
        string resumeText;
        byte[] pdfBytes;
        await using (var stream = request.CvFile.OpenReadStream())
        {
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, cancellationToken);
            pdfBytes = ms.ToArray();
            ms.Position = 0;
            resumeText = await _pdfTextExtractor.ExtractTextAsync(ms);
        }

        // 4. AI extract contact info, falling back to direct PDF analysis for hard-to-parse layouts.
        var extracted = await _cvInfoExtractor.ExtractContactInfoAsync(resumeText, pdfBytes, cancellationToken);

        _logger.LogInformation("CV info extracted for upload by HR {UserId}: Name={Name}, Email={Email}",
            userId, extracted.FullName, extracted.Email);

        return new ExtractCvInfoResult
        {
            ResumeUrl = resumeUrl,
            ResumePublicId = resumePublicId,
            ResumeText = resumeText.Length > 10000 ? resumeText[..10000] : resumeText,
            FullName = extracted.FullName,
            Email = extracted.Email,
            Phone = extracted.Phone
        };
    }
}
