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
    public decimal? SalaryRangeMin { get; set; }
    public decimal? SalaryRangeMax { get; set; }
    public bool? ShowSalary { get; set; }
}
