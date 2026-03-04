using FluentValidation;

namespace ERMS.Application.Features.JobPostings.Commands.UpdateJobPosting;

public sealed class UpdateJobPostingValidator : AbstractValidator<UpdateJobPostingCommand>
{
    public UpdateJobPostingValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Job posting ID is required.");

        RuleFor(x => x.Description)
            .MaximumLength(5000)
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Benefits)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrEmpty(x.Benefits));

        RuleFor(x => x.Location)
            .MaximumLength(200)
            .When(x => !string.IsNullOrEmpty(x.Location));

        RuleFor(x => x.ApplicationDeadline)
            .GreaterThan(DateTime.UtcNow)
            .When(x => x.ApplicationDeadline.HasValue)
            .WithMessage("Application deadline must be in the future.");
    }
}
