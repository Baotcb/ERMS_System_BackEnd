using MediatR;

namespace ERMS.Application.Features.Reports.Queries.GetReportStats;

public sealed class GetReportStatsQuery : IRequest<ReportStatsDto>
{
}

public sealed class ReportStatsDto
{
    public int TotalPending { get; set; }
    public int TotalReviewing { get; set; }
    public int TotalResolved { get; set; }
    public int TotalDismissed { get; set; }
    public int TotalJobPostingReports { get; set; }
    public int TotalEnterpriseReports { get; set; }
}

