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
    public DateTime? UpdatedAt { get; set; }

    // Creator / publisher names
    public string? CreatedByName { get; set; }
    public string? PublishedByName { get; set; }

    // Flattened navigation
    public string DepartmentName { get; set; } = null!;
    public Guid? PlanDetailId { get; set; }
    public string? PlanName { get; set; }
    public string? CampaignName { get; set; }
    public int? QuotaUsed { get; set; }
    public int? QuotaTotal { get; set; }

    // Application pipeline breakdown (all 10 stages)
    public int TotalApplications { get; set; }
    public int AppliedCount { get; set; }
    public int ReviewingCount { get; set; }
    public int ShortlistedCount { get; set; }
    public int InterviewScheduledCount { get; set; }
    public int InterviewedCount { get; set; }
    public int OfferProcessingCount { get; set; }
    public int OfferedCount { get; set; }
    public int HiredCount { get; set; }
    public int RejectedCount { get; set; }
    public int WithdrawnCount { get; set; }

    // Audit history
    public List<JobPostingHistoryDto>? History { get; set; }
}
