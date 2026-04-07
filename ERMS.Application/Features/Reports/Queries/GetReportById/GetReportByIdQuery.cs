using MediatR;

namespace ERMS.Application.Features.Reports.Queries.GetReportById;

public sealed class GetReportByIdQuery : IRequest<ReportDetailDto>
{
    public Guid Id { get; set; }
}

public sealed class ReportDetailDto
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
    public object? EntityDetails { get; set; }
    public List<RelatedReportDto> RelatedReports { get; set; } = [];
}

public sealed class RelatedReportDto
{
    public Guid Id { get; set; }
    public string Reason { get; set; } = null!;
    public string ReportedByName { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

