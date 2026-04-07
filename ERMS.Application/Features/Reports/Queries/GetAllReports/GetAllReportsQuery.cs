using MediatR;

namespace ERMS.Application.Features.Reports.Queries.GetAllReports;

public sealed class GetAllReportsQuery : IRequest<GetAllReportsResult>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public string? Status { get; set; }
    public string? EntityType { get; set; }
    public string? Reason { get; set; }
}

public sealed class GetAllReportsResult
{
    public List<ReportListDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public sealed class ReportListDto
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = null!;
    public Guid EntityId { get; set; }
    public string EntityName { get; set; } = null!;
    public string Reason { get; set; } = null!;
    public string? Description { get; set; }
    public string Status { get; set; } = null!;
    public string ReportedByName { get; set; } = null!;
    public string ReportedByEmail { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string? ResolvedByName { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? AdminNote { get; set; }
    public string? ActionTaken { get; set; }
    public int ReportCountForEntity { get; set; }
}

