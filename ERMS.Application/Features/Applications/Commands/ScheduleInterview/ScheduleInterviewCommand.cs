using MediatR;

namespace ERMS.Application.Features.Applications.Commands.ScheduleInterview;

/// <summary>
/// Command to schedule an interview for a shortlisted application
/// Restricted to DepartmentHead of the same department as the job posting
/// </summary>
public sealed record ScheduleInterviewCommand : IRequest<ScheduleInterviewResult>
{
    public Guid ApplicationId { get; init; }
    public DateTime ScheduledAt { get; init; }
    public int Duration { get; init; } = 60;
    public string? Location { get; init; }
    public string? MeetingLink { get; init; }
    public string? Note { get; init; }
    public string InterviewType { get; init; } = "Technical";
    public List<Guid> InterviewerIds { get; init; } = [];
}
