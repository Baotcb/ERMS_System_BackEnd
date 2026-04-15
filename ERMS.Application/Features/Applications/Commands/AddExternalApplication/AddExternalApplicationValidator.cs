using FluentValidation;

namespace ERMS.Application.Features.Applications.Commands.AddExternalApplication;

public sealed class AddExternalApplicationValidator : AbstractValidator<AddExternalApplicationCommand>
{
    public AddExternalApplicationValidator()
    {
        RuleFor(x => x.JobPostingId)
            .NotEmpty().WithMessage("JobPostingId là bắt buộc.");

        RuleFor(x => x.CandidateName)
            .NotEmpty().WithMessage("Tên ứng viên là bắt buộc.")
            .MaximumLength(200).WithMessage("Tên ứng viên không được vượt quá 200 ký tự.");

        RuleFor(x => x.CandidateEmail)
            .NotEmpty().WithMessage("Email ứng viên là bắt buộc.")
            .EmailAddress().WithMessage("Email ứng viên không hợp lệ.")
            .MaximumLength(256);

        RuleFor(x => x.CandidatePhone)
            .MaximumLength(20)
            .When(x => !string.IsNullOrWhiteSpace(x.CandidatePhone));

        RuleFor(x => x.ResumeUrl)
            .NotEmpty().WithMessage("ResumeUrl là bắt buộc.");

        RuleFor(x => x.ResumeText)
            .NotEmpty().WithMessage("ResumeText là bắt buộc.");
    }
}
