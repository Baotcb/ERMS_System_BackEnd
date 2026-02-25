namespace ERMS.Application.Features.Applications.Commands.AssignInterviewer;

public class AssignInterviewerResult
{
    public Guid InterviewId { get; set; }
    public Guid ApplicationId { get; set; }
    public string InterviewType { get; set; } = null!;
    public string Status { get; set; } = null!;
    public List<InterviewParticipantDto> Participants { get; set; } = [];
}

public class InterviewParticipantDto
{
    public Guid ParticipantId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = null!;
    public string Role { get; set; } = null!;
    public string ConfirmationStatus { get; set; } = null!;
}
