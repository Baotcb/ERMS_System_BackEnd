using MediatR;

namespace ERMS.Application.Features.Applications.Queries.GetAllApplications;

/// <summary>
/// Query to retrieve all applications across the enterprise for HR Manager
/// </summary>
public sealed class GetAllApplicationsQuery : IRequest<GetAllApplicationsResponse>
{
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
