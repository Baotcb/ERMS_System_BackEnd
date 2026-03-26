using ERMS.Application.Features.Admin;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Enterprise;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Admin.Queries.GetPlatformStats;

public sealed class GetPlatformStatsHandler : IRequestHandler<GetPlatformStatsQuery, GetPlatformStatsResponse>
{
    private readonly IERMSDbContext _context;

    public GetPlatformStatsHandler(IERMSDbContext context)
    {
        _context = context;
    }

    public async Task<GetPlatformStatsResponse> Handle(GetPlatformStatsQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var expiringThreshold = now.AddDays(30);
        var enterprises = _context.Enterprises
            .AsNoTracking()
            .Where(enterprise => !enterprise.IsDeleted);

        var snapshots = await enterprises
            .Select(enterprise => new
            {
                enterprise.Id,
                enterprise.EnterpriseName,
                enterprise.Status,
                enterprise.SubscriptionEndDate,
                PlanName = enterprise.SubscriptionPlan.PlanName,
                PlanCode = enterprise.SubscriptionPlan.PlanCode,
                PriceMonthly = enterprise.SubscriptionPlan.PriceMonthly
            })
            .ToListAsync(cancellationToken);

        var employeeCounts = await _context.Employees
            .AsNoTracking()
            .Where(employee => !employee.IsDeleted)
            .GroupBy(employee => employee.EnterpriseId)
            .ToDictionaryAsync(group => group.Key, group => group.Count(), cancellationToken);

        var renewalWindowStart = now.AddDays(-30);
        var renewingEnterpriseIds = await _context.SubscriptionHistories
            .AsNoTracking()
            .Where(history => history.ActionType == "Renew" && history.CreatedAt >= renewalWindowStart && history.CreatedAt <= now)
            .Select(history => history.EnterpriseId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var dueEnterpriseIds = await _context.SubscriptionHistories
            .AsNoTracking()
            .Where(history => history.PeriodEndDate >= renewalWindowStart && history.PeriodEndDate <= now)
            .Select(history => history.EnterpriseId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var renewalRate = dueEnterpriseIds.Count == 0
            ? 0
            : Math.Round(
                dueEnterpriseIds.Intersect(renewingEnterpriseIds).Count() * 100m / dueEnterpriseIds.Count,
                1);

        return new GetPlatformStatsResponse
        {
            TotalEnterprises = snapshots.Count,
            ActiveEnterprises = snapshots.Count(enterprise => enterprise.Status == EnterpriseStatus.Active),
            LockedEnterprises = snapshots.Count(enterprise => enterprise.Status == EnterpriseStatus.Locked),
            SuspendedEnterprises = snapshots.Count(enterprise => enterprise.Status == EnterpriseStatus.Suspended),
            InactiveEnterprises = snapshots.Count(enterprise => enterprise.Status == EnterpriseStatus.Inactive),
            MrrCurrentMonth = snapshots
                .Where(enterprise =>
                    enterprise.Status == EnterpriseStatus.Active &&
                    enterprise.SubscriptionEndDate >= now &&
                    EnterprisePlanTier.GetTierName(enterprise.PlanName, enterprise.PlanCode) == EnterprisePlanTier.Pro)
                .Sum(enterprise => enterprise.PriceMonthly),
            RenewalRate = renewalRate,
            StatusDistribution =
            [
                new PlatformStatusDistributionDto
                {
                    Status = EnterpriseStatus.Active,
                    Count = snapshots.Count(enterprise => enterprise.Status == EnterpriseStatus.Active)
                },
                new PlatformStatusDistributionDto
                {
                    Status = EnterpriseStatus.Suspended,
                    Count = snapshots.Count(enterprise => enterprise.Status == EnterpriseStatus.Suspended)
                },
                new PlatformStatusDistributionDto
                {
                    Status = EnterpriseStatus.Locked,
                    Count = snapshots.Count(enterprise => enterprise.Status == EnterpriseStatus.Locked)
                },
                new PlatformStatusDistributionDto
                {
                    Status = EnterpriseStatus.Inactive,
                    Count = snapshots.Count(enterprise => enterprise.Status == EnterpriseStatus.Inactive)
                }
            ],
            SubscriptionMix =
            [
                new PlatformSubscriptionMixDto
                {
                    TierName = EnterprisePlanTier.Free,
                    Count = snapshots.Count(enterprise => EnterprisePlanTier.GetTierName(enterprise.PlanName, enterprise.PlanCode) == EnterprisePlanTier.Free)
                },
                new PlatformSubscriptionMixDto
                {
                    TierName = EnterprisePlanTier.Pro,
                    Count = snapshots.Count(enterprise => EnterprisePlanTier.GetTierName(enterprise.PlanName, enterprise.PlanCode) == EnterprisePlanTier.Pro)
                }
            ],
            TopEnterprises = snapshots
                .Select(enterprise => new TopEnterpriseDto
                {
                    EnterpriseId = enterprise.Id,
                    EnterpriseName = enterprise.EnterpriseName,
                    Metric = "nhan vien",
                    Value = employeeCounts.TryGetValue(enterprise.Id, out var employeeCount) ? employeeCount : 0
                })
                .OrderByDescending(enterprise => enterprise.Value)
                .ThenBy(enterprise => enterprise.EnterpriseName)
                .Take(5)
                .ToList(),
            ChurnWatchlist = snapshots
                .Select(enterprise => new
                {
                    enterprise.Id,
                    enterprise.EnterpriseName,
                    enterprise.Status,
                    enterprise.SubscriptionEndDate,
                    RiskReason = GetRiskReason(enterprise.Status, enterprise.SubscriptionEndDate, now, expiringThreshold),
                    Priority = GetRiskPriority(enterprise.Status, enterprise.SubscriptionEndDate, now, expiringThreshold)
                })
                .Where(enterprise => enterprise.Priority.HasValue && enterprise.RiskReason != null)
                .OrderBy(enterprise => enterprise.Priority)
                .ThenBy(enterprise => enterprise.SubscriptionEndDate)
                .Take(5)
                .Select(enterprise => new ChurnWatchlistItemDto
                {
                    EnterpriseId = enterprise.Id,
                    EnterpriseName = enterprise.EnterpriseName,
                    Status = enterprise.Status,
                    RiskReason = enterprise.RiskReason!,
                    SubscriptionEndDate = enterprise.SubscriptionEndDate
                })
                .ToList()
        };
    }

    private static int? GetRiskPriority(string status, DateTime subscriptionEndDate, DateTime now, DateTime expiringThreshold)
    {
        return status switch
        {
            EnterpriseStatus.Locked => 0,
            EnterpriseStatus.Inactive => 1,
            EnterpriseStatus.Suspended => 2,
            EnterpriseStatus.Active when subscriptionEndDate < now => 3,
            EnterpriseStatus.Active when subscriptionEndDate <= expiringThreshold => 4,
            _ => null
        };
    }

    private static string? GetRiskReason(string status, DateTime subscriptionEndDate, DateTime now, DateTime expiringThreshold)
    {
        return status switch
        {
            EnterpriseStatus.Locked => "Doanh nghiep dang bi khoa",
            EnterpriseStatus.Inactive => "Doanh nghiep da ngung hoat dong",
            EnterpriseStatus.Suspended => "Doanh nghiep dang tam dung",
            EnterpriseStatus.Active when subscriptionEndDate < now => "Subscription da het han",
            EnterpriseStatus.Active when subscriptionEndDate <= expiringThreshold =>
                $"Subscription sap het han trong {Math.Max(0, (subscriptionEndDate.Date - now.Date).Days)} ngay",
            _ => null
        };
    }
}
