using ERMS.Domain.Enums;
using MediatR;

namespace ERMS.Application.Features.Applications.Commands.ConfirmInterviewSchedule;

/// <summary>
/// Command to confirm the schedule for a pending interview.
/// Restricted to HRManager.
/// </summary>
public sealed record ConfirmInterviewScheduleCommand : IRequest<ConfirmInterviewScheduleResult>
{
    public Guid ApplicationId { get; init; }
    public InterviewFormat InterviewFormat { get; init; } = InterviewFormat.Online;
    public DateTime ScheduledAt { get; init; }
    public int Duration { get; init; } = 60;
    public string? Location { get; init; }
    public string? MeetingLink { get; init; }
}
