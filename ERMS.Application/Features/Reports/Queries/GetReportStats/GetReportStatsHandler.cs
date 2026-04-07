using ERMS.Application.Interface;
using ERMS.Domain.Constants.System;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Reports.Queries.GetReportStats;

public sealed class GetReportStatsHandler : IRequestHandler<GetReportStatsQuery, ReportStatsDto>
{
    private readonly IERMSDbContext _context;

    public GetReportStatsHandler(IERMSDbContext context)
    {
        _context = context;
    }

    public async Task<ReportStatsDto> Handle(GetReportStatsQuery request, CancellationToken cancellationToken)
    {
        var aggregated = await _context.Reports
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(group => new
            {
                TotalPending = group.Count(x => x.Status == ReportConstants.Status.Pending),
                TotalReviewing = group.Count(x => x.Status == ReportConstants.Status.Reviewing),
                TotalResolved = group.Count(x => x.Status == ReportConstants.Status.Resolved),
                TotalDismissed = group.Count(x => x.Status == ReportConstants.Status.Dismissed),
                TotalJobPostingReports = group.Count(x => x.EntityType == ReportConstants.EntityType.JobPosting),
                TotalEnterpriseReports = group.Count(x => x.EntityType == ReportConstants.EntityType.Enterprise)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return new ReportStatsDto
        {
            TotalPending = aggregated?.TotalPending ?? 0,
            TotalReviewing = aggregated?.TotalReviewing ?? 0,
            TotalResolved = aggregated?.TotalResolved ?? 0,
            TotalDismissed = aggregated?.TotalDismissed ?? 0,
            TotalJobPostingReports = aggregated?.TotalJobPostingReports ?? 0,
            TotalEnterpriseReports = aggregated?.TotalEnterpriseReports ?? 0
        };
    }
}
