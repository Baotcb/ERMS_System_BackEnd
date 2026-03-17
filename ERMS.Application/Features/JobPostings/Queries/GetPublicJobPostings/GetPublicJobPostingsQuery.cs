using MediatR;

namespace ERMS.Application.Features.JobPostings.Queries.GetPublicJobPostings;

/// <summary>
/// Query to get published job postings for public access (Guests and Candidates)
/// </summary>
public sealed class GetPublicJobPostingsQuery : IRequest<GetPublicJobPostingsResponse>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    
    /// <summary>
    /// Optional search term for job title or description
    /// </summary>
    public string? SearchTerm { get; set; }
    
    /// <summary>
    /// Optional filter by location
    /// </summary>
    public string? Location { get; set; }
    
    /// <summary>
    /// Optional filter by employment type (FullTime, PartTime, Contract)
    /// </summary>
    public string? EmploymentType { get; set; }

    /// <summary>
    /// Optional filter by normalized experience bucket (0, 0-1, 1-2, 2-3, 3-5, 5+)
    /// </summary>
    public string? ExperienceBucket { get; set; }

    /// <summary>
    /// Optional salary lower bound
    /// </summary>
    public decimal? MinSalary { get; set; }

    /// <summary>
    /// Optional salary upper bound
    /// </summary>
    public decimal? MaxSalary { get; set; }

    /// <summary>
    /// Optional filter by department ID
    /// </summary>
    public int? DepartmentId { get; set; }

    /// <summary>
    /// Sort option: newest, salary_desc, relevance
    /// </summary>
    public string? SortBy { get; set; }
    
    /// <summary>
    /// Optional filter by enterprise ID (for multi-tenant job board)
    /// </summary>
    public Guid? EnterpriseId { get; set; }
}
