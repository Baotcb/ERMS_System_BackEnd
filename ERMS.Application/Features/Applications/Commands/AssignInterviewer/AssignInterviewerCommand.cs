using MediatR;

namespace ERMS.Application.Features.Applications.Commands.AssignInterviewer;

/// <summary>
/// Command to assign interviewers to a shortlisted application.
/// Restricted to DepartmentHead of the same department as the job posting.
/// </summary>
public sealed record AssignInterviewerCommand : IRequest<AssignInterviewerResult>
{
    public Guid ApplicationId { get; init; }
    public string? Note { get; init; }
    public string InterviewType { get; init; } = "Technical";
    public List<Guid> InterviewerIds { get; init; } = [];
}
