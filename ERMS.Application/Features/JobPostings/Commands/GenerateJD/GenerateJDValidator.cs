using FluentValidation;

namespace ERMS.Application.Features.JobPostings.Commands.GenerateJD;

public sealed class GenerateJDValidator : AbstractValidator<GenerateJDCommand>
{
    public GenerateJDValidator()
    {
        RuleFor(x => x.PlanDetailId)
            .NotEmpty()
            .WithMessage("PlanDetailId là bắt buộc.");
    }
}
