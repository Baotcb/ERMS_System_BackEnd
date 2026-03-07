using FluentValidation;

namespace ERMS.Application.Features.Applications.Commands.AssignInterviewer;

/// <summary>
/// Validator for AssignInterviewerCommand
/// </summary>
public sealed class AssignInterviewerValidator : AbstractValidator<AssignInterviewerCommand>
{
    private const int MaxNoteLength = 2000;

    public AssignInterviewerValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty()
            .WithMessage("ApplicationId là bắt buộc.");

        RuleFor(x => x.InterviewType)
            .NotEmpty()
            .WithMessage("Loại phỏng vấn là bắt buộc.")
            .MaximumLength(50)
            .WithMessage("Loại phỏng vấn không được vượt quá 50 ký tự.");

        RuleFor(x => x.InterviewerIds)
            .NotEmpty()
            .WithMessage("Cần ít nhất một người phỏng vấn.")
            .Must(ids => ids.All(id => id != Guid.Empty))
            .WithMessage("InterviewerIds không được chứa GUID rỗng.");

        RuleFor(x => x.Note)
            .MaximumLength(MaxNoteLength)
            .When(x => !string.IsNullOrEmpty(x.Note))
            .WithMessage($"Ghi chú không được vượt quá {MaxNoteLength} ký tự.");
    }
}
