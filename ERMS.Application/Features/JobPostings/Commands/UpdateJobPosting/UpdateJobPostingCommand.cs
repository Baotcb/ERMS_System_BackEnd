using MediatR;

namespace ERMS.Application.Features.JobPostings.Commands.UpdateJobPosting;

public sealed class UpdateJobPostingCommand : IRequest<Unit>
{
    public Guid Id { get; set; }
    // Note: PlanDetailId is NOT here - it is immutable

    // Draft-only fields
    public string? JobTitle { get; set; }
    public string? Requirements { get; set; }
    public string? EmploymentType { get; set; }
    public string? ExperienceLevel { get; set; }
    public string? EducationLevel { get; set; }

    // Draft + Published fields
    public string? Description { get; set; }
    public string? Benefits { get; set; }
    public DateTime? ApplicationDeadline { get; set; }
    public string? Location { get; set; }
    public string? RemoteOption { get; set; }
    public decimal? SalaryRangeMin { get; set; }
    public decimal? SalaryRangeMax { get; set; }
    public bool? ShowSalary { get; set; }
    public int? Quantity { get; set; }
}
