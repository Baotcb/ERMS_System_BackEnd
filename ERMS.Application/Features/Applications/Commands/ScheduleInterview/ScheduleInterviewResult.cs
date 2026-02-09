namespace ERMS.Application.Features.Applications.Commands.ScheduleInterview;

/// <summary>
/// Result of scheduling an interview
/// </summary>
public sealed record ScheduleInterviewResult
{
    public Guid InterviewId { get; init; }
    public Guid ApplicationId { get; init; }
    public string PreviousStage { get; init; } = string.Empty;
    public string NewStage { get; init; } = string.Empty;
    public DateTime ScheduledAt { get; init; }
    public int Duration { get; init; }
    public string InterviewType { get; init; } = string.Empty;
    public string? Location { get; init; }
    public string? MeetingLink { get; init; }
    public int RoundNumber { get; init; }
    public IReadOnlyList<InterviewParticipantDto> Participants { get; init; } = [];
}

/// <summary>
/// DTO for interview participant details
/// </summary>
public sealed record InterviewParticipantDto
{
    public Guid ParticipantId { get; init; }
    public Guid EmployeeId { get; init; }
    public string EmployeeName { get; init; } = string.Empty;
    public string Role { get; init; } = "Interviewer";
    public string ConfirmationStatus { get; init; } = "Pending";
}
