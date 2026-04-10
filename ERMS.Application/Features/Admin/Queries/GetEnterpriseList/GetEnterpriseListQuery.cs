using MediatR;

namespace ERMS.Application.Features.Admin.Queries.GetEnterpriseList;

public sealed class GetEnterpriseListQuery : IRequest<GetEnterpriseListResponse>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public string? Status { get; set; }
    public string? PlanTier { get; set; }
    public string? PlanCode { get; set; }
    public int? ExpiringWithinDays { get; set; }
}
