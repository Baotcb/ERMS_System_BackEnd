using MediatR;

namespace ERMS.Application.Features.Applications.Queries.GetShortlistedApplications;

/// <summary>
/// Query to get shortlisted applications for a job posting
/// Restricted to DepartmentHead of the same department as the job posting
/// </summary>
public sealed record GetShortlistedApplicationsQuery : IRequest<GetShortlistedApplicationsResponse>
{
    public Guid JobPostingId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
