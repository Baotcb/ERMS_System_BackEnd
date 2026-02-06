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
            .WithMessage("JobPostingId is required.");

        RuleFor(x => x.CvFile)
            .NotNull()
            .WithMessage("CV file is required.")
            .Must(file => file != null && file.Length > 0)
            .WithMessage("CV file cannot be empty.")
            .Must(file => file != null && file.Length <= MaxFileSizeBytes)
            .WithMessage("CV file size must not exceed 5MB.")
            .Must(file => file != null && IsPdfFile(file.FileName, file.ContentType))
            .WithMessage("CV file must be a PDF document.");

        RuleFor(x => x.ExpectedSalary)
            .GreaterThan(0)
            .When(x => x.ExpectedSalary.HasValue)
            .WithMessage("Expected salary must be a positive value.");

        RuleFor(x => x.AvailableStartDate)
            .GreaterThanOrEqualTo(DateTime.UtcNow.Date)
            .When(x => x.AvailableStartDate.HasValue)
            .WithMessage("Available start date cannot be in the past.");

        RuleFor(x => x.CoverLetter)
            .MaximumLength(5000)
            .When(x => !string.IsNullOrEmpty(x.CoverLetter))
            .WithMessage("Cover letter must not exceed 5000 characters.");
    }

    private static bool IsPdfFile(string fileName, string contentType)
    {
        var isPdfExtension = fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
        var isPdfContentType = contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);
        return isPdfExtension || isPdfContentType;
    }
}
