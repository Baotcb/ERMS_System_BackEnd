using ERMS.Application.Features.Interviews.Queries.GetAllInterviews;
using MediatR;

namespace ERMS.Application.Features.Interviews.Queries.GetAllInterviews;

/// <summary>
/// Query for HR to retrieve all interviews within their enterprise that have been assigned.
/// </summary>
public sealed class GetAllInterviewsQuery : IRequest<GetAllInterviewsResponse>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    
    /// <summary>
    /// Optional filter for interview status. Evaluates exact match.
    /// </summary>
    public string? StatusFilter { get; set; }
}
