using MediatR;

namespace ERMS.Application.Features.Applications.Queries.GetMyApplications;

/// <summary>
/// Query for a candidate to retrieve their own application history
/// </summary>
public sealed class GetMyApplicationsQuery : IRequest<GetMyApplicationsResponse>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? StageFilter { get; set; }
}
