using MediatR;

namespace ERMS.Application.Features.JobPostings.Commands.UpdateJobPosting;

public sealed class UpdateJobPostingCommand : IRequest<Unit>
{
    public Guid Id { get; set; }
    public string? Description { get; set; }
    public string? Benefits { get; set; }
    public DateTime? ApplicationDeadline { get; set; }
    public string? Location { get; set; }
    public string? RemoteOption { get; set; }
    // Note: PlanDetailId is NOT here - it is immutable
}
