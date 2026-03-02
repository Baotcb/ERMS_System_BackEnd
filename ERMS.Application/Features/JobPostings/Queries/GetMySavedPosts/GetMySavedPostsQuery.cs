using MediatR;

namespace ERMS.Application.Features.JobPostings.Queries.GetMySavedPosts;

/// <summary>
/// Query for a candidate to retrieve their saved job postings
/// </summary>
public sealed class GetMySavedPostsQuery : IRequest<GetMySavedPostsResponse>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
