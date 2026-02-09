namespace ERMS.Application.Features.JobPostings.Queries.GetJobPostingById;

public sealed class JobPostingDetailDto
{
    public Guid Id { get; set; }
    public string JobTitle { get; set; } = null!;
    public string? JobCode { get; set; }
    public string Description { get; set; } = null!;
    public string? Requirements { get; set; }
    public string? Benefits { get; set; }
    public string EmploymentType { get; set; } = null!;
    public string? ExperienceLevel { get; set; }
    public string? EducationLevel { get; set; }
    public decimal? SalaryRangeMin { get; set; }
    public decimal? SalaryRangeMax { get; set; }
    public bool ShowSalary { get; set; }
    public string? Location { get; set; }
    public string? RemoteOption { get; set; }
    public int Quantity { get; set; }
    public DateTime? ApplicationDeadline { get; set; }
    public string Status { get; set; } = null!;
    public DateTime? PublishedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public int ViewCount { get; set; }
    public int ApplicationCount { get; set; }
    public DateTime CreatedAt { get; set; }

    // Flattened navigation
    public string DepartmentName { get; set; } = null!;
    public Guid? PlanDetailId { get; set; }
    public string? PlanName { get; set; }
    public string? CampaignName { get; set; }
    public int? QuotaUsed { get; set; }
    public int? QuotaTotal { get; set; }
}
