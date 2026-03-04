namespace ERMS.Application.Features.JobPostings.Queries.GetPublicJobPostingById;

/// <summary>
/// Detailed public job posting DTO
/// </summary>
public sealed class PublicJobPostingDetailDto
{
    public Guid Id { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string? JobCode { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Requirements { get; set; }
    public string? Benefits { get; set; }
    public string EmploymentType { get; set; } = string.Empty;
    public string? ExperienceLevel { get; set; }
    public string? EducationLevel { get; set; }
    public decimal? SalaryRangeMin { get; set; }
    public decimal? SalaryRangeMax { get; set; }
    public bool ShowSalary { get; set; }
    public string? Location { get; set; }
    public string? RemoteOption { get; set; }
    public int Quantity { get; set; }
    public DateTime? ApplicationDeadline { get; set; }
    public DateTime? PublishedAt { get; set; }
    public int ViewCount { get; set; }
    
    // Enterprise info
    public Guid EnterpriseId { get; set; }
    public string EnterpriseName { get; set; } = string.Empty;
    public string? EnterpriseLogoUrl { get; set; }
    public string? EnterpriseWebsite { get; set; }
    
    // Department info
    public string DepartmentName { get; set; } = string.Empty;
}
