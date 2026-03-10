using FluentValidation;

namespace ERMS.Application.Features.Applications.Commands.SubmitApplication;

/// <summary>
/// Validator for SubmitApplicationCommand
/// </summary>
public sealed class SubmitApplicationValidator : AbstractValidator<SubmitApplicationCommand>
{
    private const int MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB

    public SubmitApplicationValidator()
    {
        RuleFor(x => x.JobPostingId)
            .NotEmpty()
            .WithMessage("JobPostingId là bắt buộc.");

        RuleFor(x => x.CvFile)
            .NotNull()
            .WithMessage("File CV là bắt buộc.")
            .Must(file => file != null && file.Length > 0)
            .WithMessage("File CV không được trống.")
            .Must(file => file != null && file.Length <= MaxFileSizeBytes)
            .WithMessage("Dung lượng file CV không được vượt quá 5MB.")
            .Must(file => file != null && IsPdfFile(file.FileName, file.ContentType))
            .WithMessage("File CV phải là định dạng PDF.");

        RuleFor(x => x.ExpectedSalary)
            .GreaterThanOrEqualTo(0)
            .When(x => x.ExpectedSalary.HasValue)
            .WithMessage("Mức lương mong muốn không được là số âm.");

        RuleFor(x => x.AvailableStartDate)
            .GreaterThanOrEqualTo(DateTime.UtcNow.Date)
            .When(x => x.AvailableStartDate.HasValue)
            .WithMessage("Ngày có thể bắt đầu không được trong quá khứ.");

        RuleFor(x => x.CoverLetter)
            .MaximumLength(5000)
            .When(x => !string.IsNullOrEmpty(x.CoverLetter))
            .WithMessage("Thư xin việc không được vượt quá 5000 ký tự.");
    }

    private static bool IsPdfFile(string fileName, string contentType)
    {
        var isPdfExtension = fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
        var isPdfContentType = contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);
        return isPdfExtension || isPdfContentType;
    }
}
