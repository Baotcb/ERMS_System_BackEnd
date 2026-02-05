using MediatR;

namespace ERMS.Application.Features.JobPostings.Commands.CreateJobPosting;

public sealed class CreateJobPostingCommand : IRequest<Guid>
{
    public Guid PlanDetailId { get; set; }
    public DateTime ApplicationDeadline { get; set; }
    public string? TitleOverride { get; set; }
    public string? DescriptionOverride { get; set; }
    public string? Benefits { get; set; }
    public string? Location { get; set; }
    public string? RemoteOption { get; set; }
}
