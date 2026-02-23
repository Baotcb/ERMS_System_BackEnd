using ERMS.Domain.Enums;
using FluentValidation;

namespace ERMS.Application.Features.Applications.Commands.ConfirmInterviewSchedule;

public sealed class ConfirmInterviewScheduleValidator : AbstractValidator<ConfirmInterviewScheduleCommand>
{
    private const int MinDuration = 15;
    private const int MaxDuration = 480;
    private const int MaxLocationLength = 500;
    private const int MaxMeetingLinkLength = 2048;

    public ConfirmInterviewScheduleValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty()
            .WithMessage("ApplicationId is required.");

        RuleFor(x => x.InterviewFormat)
            .IsInEnum()
            .WithMessage("InterviewFormat must be a valid value (Online or Offline).");

        RuleFor(x => x.ScheduledAt)
            .NotEmpty()
            .WithMessage("ScheduledAt is required.")
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("ScheduledAt must be in the future.");

        RuleFor(x => x.Duration)
            .InclusiveBetween(MinDuration, MaxDuration)
            .WithMessage($"Duration must be between {MinDuration} and {MaxDuration} minutes.");

        RuleFor(x => x.Location)
            .MaximumLength(MaxLocationLength)
            .When(x => !string.IsNullOrEmpty(x.Location))
            .WithMessage($"Location must not exceed {MaxLocationLength} characters.");

        RuleFor(x => x.Location)
            .NotEmpty()
            .When(x => x.InterviewFormat == InterviewFormat.Offline)
            .WithMessage("Location is required for offline interviews.");

        RuleFor(x => x.MeetingLink)
            .NotEmpty()
            .When(x => x.InterviewFormat == InterviewFormat.Online)
            .WithMessage("MeetingLink is required for online interviews.");

        RuleFor(x => x.MeetingLink)
            .Must(link => Uri.TryCreate(link, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .When(x => !string.IsNullOrEmpty(x.MeetingLink))
            .WithMessage("MeetingLink must be a valid HTTP/HTTPS URL.");

        RuleFor(x => x.MeetingLink)
            .MaximumLength(MaxMeetingLinkLength)
            .When(x => !string.IsNullOrEmpty(x.MeetingLink))
            .WithMessage($"MeetingLink must not exceed {MaxMeetingLinkLength} characters.");
    }
}
