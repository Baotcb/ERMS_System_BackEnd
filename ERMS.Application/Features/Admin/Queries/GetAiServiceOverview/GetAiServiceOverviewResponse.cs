namespace ERMS.Application.Features.Admin.Queries.GetAiServiceOverview;

public sealed class GetAiServiceOverviewResponse
{
    public string ProviderName { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public bool ApiKeyConfigured { get; set; }
    public string ConfigurationStatus { get; set; } = string.Empty;
    public string ServiceMode { get; set; } = "System-wide";
    public int ScoredToday { get; set; }
    public int ScoredLast7Days { get; set; }
    public int ScoredLast30Days { get; set; }
    public int DistinctEnterprisesLast30Days { get; set; }
    public decimal AverageScoreLast30Days { get; set; }
    public DateTime? LastProcessedAt { get; set; }
    public List<AiDailyVolumeDto> DailyVolumes { get; set; } = [];
    public List<AiScoreBucketDto> ScoreDistribution { get; set; } = [];
}

public sealed class AiDailyVolumeDto
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}

public sealed class AiScoreBucketDto
{
    public string Bucket { get; set; } = string.Empty;
    public int Count { get; set; }
}
