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
            .WithMessage("ApplicationId là bắt buộc.");

        RuleFor(x => x.InterviewFormat)
            .IsInEnum()
            .WithMessage("Hình thức phỏng vấn phải hợp lệ (Online hoặc Offline).");

        RuleFor(x => x.ScheduledAt)
            .NotEmpty()
            .WithMessage("Thời gian phỏng vấn là bắt buộc.")
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Thời gian phỏng vấn phải trong tương lai.");

        RuleFor(x => x.Duration)
            .InclusiveBetween(MinDuration, MaxDuration)
            .WithMessage($"Thời lượng phải từ {MinDuration} đến {MaxDuration} phút.");

        RuleFor(x => x.Location)
            .MaximumLength(MaxLocationLength)
            .When(x => !string.IsNullOrEmpty(x.Location))
            .WithMessage($"Địa điểm không được vượt quá {MaxLocationLength} ký tự.");

        RuleFor(x => x.Location)
            .NotEmpty()
            .When(x => x.InterviewFormat == InterviewFormat.Offline)
            .WithMessage("Địa điểm là bắt buộc cho phỏng vấn trực tiếp.");

        // MeetingLink is optional for Online format — Handler auto-creates Zoom if empty

        RuleFor(x => x.MeetingLink)
            .Must(link => Uri.TryCreate(link, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .When(x => !string.IsNullOrEmpty(x.MeetingLink))
            .WithMessage("Link cuộc hỌp phải là URL HTTP/HTTPS hợp lệ.");

        RuleFor(x => x.MeetingLink)
            .MaximumLength(MaxMeetingLinkLength)
            .When(x => !string.IsNullOrEmpty(x.MeetingLink))
            .WithMessage($"Link cuộc hỌp không được vượt quá {MaxMeetingLinkLength} ký tự.");
    }
}
