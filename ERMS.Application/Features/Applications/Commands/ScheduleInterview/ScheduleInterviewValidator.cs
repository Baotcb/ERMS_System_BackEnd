using FluentValidation;

namespace ERMS.Application.Features.Applications.Commands.ScheduleInterview;

/// <summary>
/// Validator for ScheduleInterviewCommand
/// </summary>
public sealed class ScheduleInterviewValidator : AbstractValidator<ScheduleInterviewCommand>
{
    private const int MaxNoteLength = 2000;
    private const int MaxLocationLength = 500;
    private const int MaxMeetingLinkLength = 500;
    private const int MinDuration = 15;
    private const int MaxDuration = 480; // 8 hours max

    public ScheduleInterviewValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty()
            .WithMessage("ApplicationId is required.");

        RuleFor(x => x.ScheduledAt)
            .NotEmpty()
            .WithMessage("ScheduledAt is required.")
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("ScheduledAt must be in the future.");

        RuleFor(x => x.Duration)
            .InclusiveBetween(MinDuration, MaxDuration)
            .WithMessage($"Duration must be between {MinDuration} and {MaxDuration} minutes.");

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

        RuleFor(x => x.Location)
            .MaximumLength(MaxLocationLength)
            .When(x => !string.IsNullOrEmpty(x.Location))
            .WithMessage($"Location must not exceed {MaxLocationLength} characters.");

        RuleFor(x => x.MeetingLink)
            .MaximumLength(MaxMeetingLinkLength)
            .When(x => !string.IsNullOrEmpty(x.MeetingLink))
            .WithMessage($"MeetingLink must not exceed {MaxMeetingLinkLength} characters.");

        RuleFor(x => x.Note)
            .MaximumLength(MaxNoteLength)
            .When(x => !string.IsNullOrEmpty(x.Note))
            .WithMessage($"Note must not exceed {MaxNoteLength} characters.");
    }
}
