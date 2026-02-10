using MediatR;

namespace ERMS.Application.Features.Applications.Queries.GetApplicationsByJob;

/// <summary>
/// Query to retrieve applications for a specific job posting, sorted by AI match score
/// </summary>
public sealed class GetApplicationsByJobQuery : IRequest<GetApplicationsByJobResponse>
{
    /// <summary>
    /// The Job Posting ID to fetch applications for
    /// </summary>
    public Guid JobPostingId { get; set; }

    /// <summary>
    /// Page number for pagination (1-indexed)
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Optional filter by application stage (e.g., "Applied", "Shortlisted")
    /// </summary>
    public string? StageFilter { get; set; }
}
