using FluentValidation;

namespace ERMS.Application.Features.JobPostings.Commands.CreateJobPosting;

public sealed class CreateJobPostingValidator : AbstractValidator<CreateJobPostingCommand>
{
    public CreateJobPostingValidator()
    {
        RuleFor(x => x.PlanDetailId)
            .NotEmpty()
            .WithMessage("PlanDetailId là bắt buộc.");

        RuleFor(x => x.ApplicationDeadline)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Hạn nộp hồ sơ phải trong tương lai.");

        RuleFor(x => x.TitleOverride)
            .MaximumLength(200)
            .When(x => !string.IsNullOrEmpty(x.TitleOverride));

        RuleFor(x => x.DescriptionOverride)
            .MaximumLength(5000)
            .When(x => !string.IsNullOrEmpty(x.DescriptionOverride));

        RuleFor(x => x.RequirementsOverride)
            .MaximumLength(5000)
            .When(x => !string.IsNullOrEmpty(x.RequirementsOverride));

        RuleFor(x => x.SalaryRangeMin)
            .GreaterThanOrEqualTo(0)
            .When(x => x.SalaryRangeMin.HasValue);

        RuleFor(x => x.SalaryRangeMax)
            .GreaterThanOrEqualTo(0)
            .When(x => x.SalaryRangeMax.HasValue);

        RuleFor(x => x.SalaryRangeMax)
            .Must((cmd, max) => !max.HasValue || !cmd.SalaryRangeMin.HasValue || max.Value >= cmd.SalaryRangeMin.Value)
            .WithMessage("SalaryRangeMax phải lớn hơn hoặc bằng SalaryRangeMin.")
            .When(x => x.SalaryRangeMax.HasValue && x.SalaryRangeMin.HasValue);
    }
}
