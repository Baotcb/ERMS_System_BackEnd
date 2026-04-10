using FluentValidation;

namespace ERMS.Application.Features.Applications.Commands.ExtractCvInfo;

public sealed class ExtractCvInfoValidator : AbstractValidator<ExtractCvInfoCommand>
{
    public ExtractCvInfoValidator()
    {
        RuleFor(x => x.CvFile)
            .NotNull().WithMessage("Vui lòng chọn file CV.");

        RuleFor(x => x.CvFile.ContentType)
            .Must(ct => ct == "application/pdf")
            .When(x => x.CvFile != null)
            .WithMessage("Chỉ chấp nhận file PDF.");

        RuleFor(x => x.CvFile.Length)
            .LessThanOrEqualTo(5 * 1024 * 1024)
            .When(x => x.CvFile != null)
            .WithMessage("Kích thước file không được vượt quá 5MB.");
    }
}
