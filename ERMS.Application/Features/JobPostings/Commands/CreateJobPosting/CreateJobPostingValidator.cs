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
    }
}
