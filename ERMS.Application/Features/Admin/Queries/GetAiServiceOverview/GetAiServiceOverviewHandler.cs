using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Admin.Queries.GetAiServiceOverview;

public sealed class GetAiServiceOverviewHandler : IRequestHandler<GetAiServiceOverviewQuery, GetAiServiceOverviewResponse>
{
    private readonly IERMSDbContext _context;
    private readonly IAIServiceConfiguration _aiServiceConfiguration;

    public GetAiServiceOverviewHandler(IERMSDbContext context, IAIServiceConfiguration aiServiceConfiguration)
    {
        _context = context;
        _aiServiceConfiguration = aiServiceConfiguration;
    }

    public async Task<GetAiServiceOverviewResponse> Handle(GetAiServiceOverviewQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var startOfToday = now.Date;
        var startOf7Days = startOfToday.AddDays(-6);
        var startOf30Days = startOfToday.AddDays(-29);

        var results = _context.CVScreeningResults
            .AsNoTracking()
            .Where(result => result.ProcessedAt >= startOf30Days);

        var summary = await results
            .GroupBy(_ => 1)
            .Select(group => new
            {
                ScoredToday = group.Count(result => result.ProcessedAt >= startOfToday),
                ScoredLast7Days = group.Count(result => result.ProcessedAt >= startOf7Days),
                ScoredLast30Days = group.Count(),
                AverageScoreLast30Days = group.Average(result => (decimal?)result.OverallScore) ?? 0,
                LastProcessedAt = group.Max(result => (DateTime?)result.ProcessedAt),
                HighScoreCount = group.Count(result => result.OverallScore >= 71),
                MediumScoreCount = group.Count(result => result.OverallScore >= 41 && result.OverallScore <= 70),
                LowScoreCount = group.Count(result => result.OverallScore <= 40)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var distinctEnterprisesLast30Days = summary == null
            ? 0
            : await results
                .Select(result => result.Application.JobPosting.EnterpriseId)
                .Distinct()
                .CountAsync(cancellationToken);
        var dailyVolumeRows = await results
            .Where(result => result.ProcessedAt >= startOf7Days)
            .GroupBy(result => result.ProcessedAt.Date)
            .Select(group => new
            {
                Date = group.Key,
                Count = group.Count()
            })
            .ToListAsync(cancellationToken);
        var dailyVolumeLookup = dailyVolumeRows.ToDictionary(item => item.Date.Date, item => item.Count);

        return new GetAiServiceOverviewResponse
        {
            ProviderName = _aiServiceConfiguration.ProviderName,
            ModelName = _aiServiceConfiguration.ModelName,
            ApiKeyConfigured = _aiServiceConfiguration.HasApiKey,
            ConfigurationStatus = _aiServiceConfiguration.HasApiKey ? "Configured" : "Missing configuration",
            ScoredToday = summary?.ScoredToday ?? 0,
            ScoredLast7Days = summary?.ScoredLast7Days ?? 0,
            ScoredLast30Days = summary?.ScoredLast30Days ?? 0,
            DistinctEnterprisesLast30Days = distinctEnterprisesLast30Days,
            AverageScoreLast30Days = summary == null ? 0 : Math.Round(summary.AverageScoreLast30Days, 1),
            LastProcessedAt = summary?.LastProcessedAt,
            DailyVolumes = Enumerable.Range(0, 7)
                .Select(offset => startOf7Days.AddDays(offset))
                .Select(date => new AiDailyVolumeDto
                {
                    Date = date,
                    Count = dailyVolumeLookup.TryGetValue(date.Date, out var count) ? count : 0
                })
                .ToList(),
            ScoreDistribution =
            [
                new AiScoreBucketDto
                {
                    Bucket = "71-100",
                    Count = summary?.HighScoreCount ?? 0
                },
                new AiScoreBucketDto
                {
                    Bucket = "41-70",
                    Count = summary?.MediumScoreCount ?? 0
                },
                new AiScoreBucketDto
                {
                    Bucket = "0-40",
                    Count = summary?.LowScoreCount ?? 0
                }
            ]
        };
    }
}
