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

        var results = await _context.CVScreeningResults
            .AsNoTracking()
            .Where(result => result.ProcessedAt >= startOf30Days)
            .Select(result => new
            {
                result.ProcessedAt,
                result.OverallScore,
                EnterpriseId = result.Application.JobPosting.EnterpriseId
            })
            .ToListAsync(cancellationToken);

        return new GetAiServiceOverviewResponse
        {
            ProviderName = _aiServiceConfiguration.ProviderName,
            ModelName = _aiServiceConfiguration.ModelName,
            ApiKeyConfigured = _aiServiceConfiguration.HasApiKey,
            ConfigurationStatus = _aiServiceConfiguration.HasApiKey ? "Configured" : "Missing configuration",
            ScoredToday = results.Count(result => result.ProcessedAt >= startOfToday),
            ScoredLast7Days = results.Count(result => result.ProcessedAt >= startOf7Days),
            ScoredLast30Days = results.Count,
            DistinctEnterprisesLast30Days = results
                .Select(result => result.EnterpriseId)
                .Distinct()
                .Count(),
            AverageScoreLast30Days = results.Count == 0
                ? 0
                : Math.Round(results.Average(result => result.OverallScore), 1),
            LastProcessedAt = results
                .OrderByDescending(result => result.ProcessedAt)
                .Select(result => (DateTime?)result.ProcessedAt)
                .FirstOrDefault(),
            DailyVolumes = Enumerable.Range(0, 7)
                .Select(offset => startOf7Days.AddDays(offset))
                .Select(date => new AiDailyVolumeDto
                {
                    Date = date,
                    Count = results.Count(result => result.ProcessedAt.Date == date.Date)
                })
                .ToList(),
            ScoreDistribution =
            [
                new AiScoreBucketDto
                {
                    Bucket = "71-100",
                    Count = results.Count(result => result.OverallScore >= 71)
                },
                new AiScoreBucketDto
                {
                    Bucket = "41-70",
                    Count = results.Count(result => result.OverallScore >= 41 && result.OverallScore <= 70)
                },
                new AiScoreBucketDto
                {
                    Bucket = "0-40",
                    Count = results.Count(result => result.OverallScore <= 40)
                }
            ]
        };
    }
}
