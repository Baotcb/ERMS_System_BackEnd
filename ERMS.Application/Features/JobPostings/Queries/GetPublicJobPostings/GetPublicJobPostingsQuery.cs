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
    /// Optional filter by enterprise ID (for multi-tenant job board)
    /// </summary>
    public Guid? EnterpriseId { get; set; }
}
