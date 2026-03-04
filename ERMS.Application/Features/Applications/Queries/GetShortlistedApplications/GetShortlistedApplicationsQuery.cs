using MediatR;

namespace ERMS.Application.Features.Applications.Queries.GetShortlistedApplications;

/// <summary>
/// Query to get shortlisted applications for a plan detail
/// Restricted to DepartmentHead of the same department as the recruitment plan
/// </summary>
public sealed record GetShortlistedApplicationsQuery : IRequest<GetShortlistedApplicationsResponse>
{
    public Guid PlanDetailId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
