namespace ERMS.Application.Features.JobPostings.Queries.GetPublicJobPostings;

/// <summary>
/// Response for public job postings list
/// </summary>
public sealed class GetPublicJobPostingsResponse
{
    public List<PublicJobPostingDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

/// <summary>
/// Public-facing job posting DTO (excludes internal information)
/// </summary>
public sealed class PublicJobPostingDto
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
    
    /// <summary>
    /// Only shown if ShowSalary is true
    /// </summary>
    public decimal? SalaryRangeMin { get; set; }
    public decimal? SalaryRangeMax { get; set; }
    public bool ShowSalary { get; set; }
    
    public string? Location { get; set; }
    public string? RemoteOption { get; set; }
    public int Quantity { get; set; }
    public DateTime? ApplicationDeadline { get; set; }
    public DateTime? PublishedAt { get; set; }
    
    // Enterprise info for public display
    public Guid EnterpriseId { get; set; }
    public string EnterpriseName { get; set; } = string.Empty;
    public string? EnterpriseLogoUrl { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
}
