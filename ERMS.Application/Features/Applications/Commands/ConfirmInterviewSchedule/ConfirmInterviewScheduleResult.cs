using ERMS.Domain.Enums;

namespace ERMS.Application.Features.Applications.Commands.ConfirmInterviewSchedule;

public class ConfirmInterviewScheduleResult
{
    public Guid InterviewId { get; set; }
    public string Status { get; set; } = null!;
    public InterviewFormat InterviewFormat { get; set; }
    public DateTime ScheduledAt { get; set; }
    public int Duration { get; set; }
    public string? MeetingLink { get; set; }
    public string? Location { get; set; }
}
