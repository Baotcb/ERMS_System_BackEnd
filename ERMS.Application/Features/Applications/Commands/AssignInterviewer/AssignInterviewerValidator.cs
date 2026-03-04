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
            .WithMessage("ApplicationId is required.");

        RuleFor(x => x.InterviewType)
            .NotEmpty()
            .WithMessage("InterviewType is required.")
            .MaximumLength(50)
            .WithMessage("InterviewType must not exceed 50 characters.");

        RuleFor(x => x.InterviewerIds)
            .NotEmpty()
            .WithMessage("At least one interviewer is required.")
            .Must(ids => ids.All(id => id != Guid.Empty))
            .WithMessage("InterviewerIds cannot contain empty GUIDs.");

        RuleFor(x => x.Note)
            .MaximumLength(MaxNoteLength)
            .When(x => !string.IsNullOrEmpty(x.Note))
            .WithMessage($"Note must not exceed {MaxNoteLength} characters.");
    }
}
