using MediatR;

namespace ERMS.Application.Features.Interviews.Queries.GetMyInterviews;

/// <summary>
/// Query to retrieve the current user's assigned interviews (as an interviewer participant)
/// </summary>
public sealed class GetMyInterviewsQuery : IRequest<GetMyInterviewsResponse>
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
    /// Optional filter by interview status (e.g., "Scheduled", "Completed", "PendingSchedule", "Cancelled")
    /// </summary>
    public string? StatusFilter { get; set; }
}
