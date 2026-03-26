namespace ERMS.Application.Features.Admin.Queries.GetPlatformStats;

public sealed class GetPlatformStatsResponse
{
    public int TotalEnterprises { get; set; }
    public int ActiveEnterprises { get; set; }
    public int LockedEnterprises { get; set; }
    public int SuspendedEnterprises { get; set; }
    public int InactiveEnterprises { get; set; }
    public decimal MrrCurrentMonth { get; set; }
    public decimal RenewalRate { get; set; }
    public List<PlatformStatusDistributionDto> StatusDistribution { get; set; } = [];
    public List<PlatformSubscriptionMixDto> SubscriptionMix { get; set; } = [];
    public List<TopEnterpriseDto> TopEnterprises { get; set; } = [];
    public List<ChurnWatchlistItemDto> ChurnWatchlist { get; set; } = [];
}

public sealed class PlatformStatusDistributionDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class PlatformSubscriptionMixDto
{
    public string TierName { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class TopEnterpriseDto
{
    public Guid EnterpriseId { get; set; }
    public string EnterpriseName { get; set; } = string.Empty;
    public string Metric { get; set; } = string.Empty;
    public int Value { get; set; }
}

public sealed class ChurnWatchlistItemDto
{
    public Guid EnterpriseId { get; set; }
    public string EnterpriseName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string RiskReason { get; set; } = string.Empty;
    public DateTime SubscriptionEndDate { get; set; }
}
