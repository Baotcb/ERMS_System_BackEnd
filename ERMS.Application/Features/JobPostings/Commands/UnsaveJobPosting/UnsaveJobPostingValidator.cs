using FluentValidation;

namespace ERMS.Application.Features.JobPostings.Commands.UnsaveJobPosting;

public sealed class UnsaveJobPostingValidator : AbstractValidator<UnsaveJobPostingCommand>
{
    public UnsaveJobPostingValidator()
    {
        RuleFor(x => x.JobPostingId)
            .NotEmpty().WithMessage("ID tin tuyển dụng là bắt buộc.");
    }
}
