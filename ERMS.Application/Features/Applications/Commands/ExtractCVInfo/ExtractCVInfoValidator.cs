using FluentValidation;

namespace ERMS.Application.Features.Applications.Commands.ExtractCVInfo;

public sealed class ExtractCVInfoValidator : AbstractValidator<ExtractCVInfoCommand>
{
    private const int MaxFileSizeBytes = 5 * 1024 * 1024;

    public ExtractCVInfoValidator()
    {
        RuleFor(x => x.CvFile)
            .NotNull().WithMessage("File CV là bắt buộc.")
            .Must(f => f != null && f.Length > 0).WithMessage("File CV không được trống.")
            .Must(f => f != null && f.Length <= MaxFileSizeBytes).WithMessage("Dung lượng file CV không được vượt quá 5MB.")
            .Must(f => f != null && IsPdf(f.FileName, f.ContentType)).WithMessage("File CV phải là định dạng PDF.");
    }

    private static bool IsPdf(string fileName, string contentType)
    {
        return fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);
    }
}
