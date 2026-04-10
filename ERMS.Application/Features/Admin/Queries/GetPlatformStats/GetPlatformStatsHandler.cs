using ERMS.Application.Features.Admin;
using ERMS.Application.Features.Admin.Queries.GetSystemIntegrations;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Enterprise;
using ERMS.Domain.Entities.Enterprise;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ERMS.Application.Features.Admin.Queries.GetPlatformStats;

public sealed class GetPlatformStatsHandler : IRequestHandler<GetPlatformStatsQuery, GetPlatformStatsResponse>
{
    private static readonly Expression<Func<Enterprise, bool>> IsProTierExpression = BuildIsProTierExpression();

    private readonly IERMSDbContext _context;
    private readonly ISystemIntegrationStatusService _integrationStatusService;

    public GetPlatformStatsHandler(IERMSDbContext context, ISystemIntegrationStatusService integrationStatusService)
    {
        _context = context;
        _integrationStatusService = integrationStatusService;
    }

    public async Task<GetPlatformStatsResponse> Handle(GetPlatformStatsQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var expiringThreshold = now.AddDays(30);
        var renewalWindowStart = now.AddDays(-30);

        var enterprises = _context.Enterprises
            .AsNoTracking()
            .Where(enterprise => !enterprise.IsDeleted);

        var proTierFilter = IsProTierExpression;

        var enterpriseSnapshots = enterprises
            .Select(enterprise => new
            {
                enterprise.Id,
                enterprise.EnterpriseName,
                enterprise.Status,
                enterprise.SubscriptionEndDate,
                PriceMonthly = enterprise.SubscriptionPlan.PriceMonthly,
                PlanName = enterprise.SubscriptionPlan.PlanName,
                PlanCode = enterprise.SubscriptionPlan.PlanCode
            });

        var proEnterpriseIds = enterprises.Where(proTierFilter).Select(e => e.Id);

        var summary = await enterpriseSnapshots
            .GroupBy(_ => 1)
            .Select(group => new
            {
                TotalEnterprises = group.Count(),
                ActiveEnterprises = group.Count(enterprise => enterprise.Status == EnterpriseStatus.Active),
                LockedEnterprises = group.Count(enterprise => enterprise.Status == EnterpriseStatus.Locked),
                SuspendedEnterprises = group.Count(enterprise => enterprise.Status == EnterpriseStatus.Suspended),
                InactiveEnterprises = group.Count(enterprise => enterprise.Status == EnterpriseStatus.Inactive),
            })
            .FirstOrDefaultAsync(cancellationToken);

        var mrrCurrentMonth = await enterprises
            .Where(proTierFilter)
            .Where(enterprise =>
                enterprise.Status == EnterpriseStatus.Active &&
                enterprise.SubscriptionEndDate >= now)
            .SumAsync(enterprise => (decimal?)enterprise.SubscriptionPlan.PriceMonthly, cancellationToken) ?? 0m;

        var proCount = await proEnterpriseIds.CountAsync(cancellationToken);
        var freeCount = Math.Max(0, (summary?.TotalEnterprises ?? 0) - proCount);

        var dueEnterpriseIds = _context.SubscriptionHistories
            .AsNoTracking()
            .Where(history => history.PeriodEndDate >= renewalWindowStart && history.PeriodEndDate <= now)
            .Select(history => history.EnterpriseId)
            .Distinct();

        var renewingEnterpriseIds = _context.SubscriptionHistories
            .AsNoTracking()
            .Where(history => history.ActionType == "Renew" && history.CreatedAt >= renewalWindowStart && history.CreatedAt <= now)
            .Select(history => history.EnterpriseId)
            .Distinct();

        var dueEnterpriseCount = await dueEnterpriseIds.CountAsync(cancellationToken);
        var renewedDueEnterpriseCount = await dueEnterpriseIds
            .Intersect(renewingEnterpriseIds)
            .CountAsync(cancellationToken);

        var renewalRate = dueEnterpriseCount == 0
            ? 0
            : Math.Round(renewedDueEnterpriseCount * 100m / dueEnterpriseCount, 1);

        var employeeCounts = _context.Employees
            .AsNoTracking()
            .Where(employee => !employee.IsDeleted)
            .GroupBy(employee => employee.EnterpriseId)
            .Select(group => new
            {
                EnterpriseId = group.Key,
                Count = group.Count()
            });

        var topEnterprises = await enterpriseSnapshots
            .GroupJoin(
                employeeCounts,
                enterprise => enterprise.Id,
                employeeCount => employeeCount.EnterpriseId,
                (enterprise, employeeGroup) => new
                {
                    enterprise.Id,
                    enterprise.EnterpriseName,
                    EmployeeCount = employeeGroup.Select(item => (int?)item.Count).FirstOrDefault() ?? 0
                })
            .OrderByDescending(enterprise => enterprise.EmployeeCount)
            .ThenBy(enterprise => enterprise.EnterpriseName)
            .Take(5)
            .Select(enterprise => new TopEnterpriseDto
            {
                EnterpriseId = enterprise.Id,
                EnterpriseName = enterprise.EnterpriseName,
                Metric = "nhân viên",
                Value = enterprise.EmployeeCount
            })
            .ToListAsync(cancellationToken);

        var churnCandidates = await enterpriseSnapshots
            .Where(enterprise =>
                enterprise.Status == EnterpriseStatus.Locked ||
                enterprise.Status == EnterpriseStatus.Inactive ||
                enterprise.Status == EnterpriseStatus.Suspended ||
                (enterprise.Status == EnterpriseStatus.Active && enterprise.SubscriptionEndDate <= expiringThreshold))
            .Select(enterprise => new
            {
                enterprise.Id,
                enterprise.EnterpriseName,
                enterprise.Status,
                enterprise.SubscriptionEndDate,
                Priority = enterprise.Status == EnterpriseStatus.Locked
                    ? 0
                    : enterprise.Status == EnterpriseStatus.Inactive
                        ? 1
                        : enterprise.Status == EnterpriseStatus.Suspended
                            ? 2
                            : enterprise.Status == EnterpriseStatus.Active && enterprise.SubscriptionEndDate < now
                                ? 3
                                : enterprise.Status == EnterpriseStatus.Active && enterprise.SubscriptionEndDate <= expiringThreshold
                                    ? 4
                                    : (int?)null
            })
            .Where(enterprise => enterprise.Priority.HasValue)
            .OrderBy(enterprise => enterprise.Priority)
            .ThenBy(enterprise => enterprise.SubscriptionEndDate)
            .Take(5)
            .ToListAsync(cancellationToken);

        var integrations = await _integrationStatusService.GetSystemIntegrationsAsync(cancellationToken);
        var integrationHealth = ComputeIntegrationHealth(integrations);

        return new GetPlatformStatsResponse
        {
            TotalEnterprises = summary?.TotalEnterprises ?? 0,
            ActiveEnterprises = summary?.ActiveEnterprises ?? 0,
            LockedEnterprises = summary?.LockedEnterprises ?? 0,
            SuspendedEnterprises = summary?.SuspendedEnterprises ?? 0,
            InactiveEnterprises = summary?.InactiveEnterprises ?? 0,
            MrrCurrentMonth = mrrCurrentMonth,
            RenewalRate = renewalRate,
            StatusDistribution =
            [
                new PlatformStatusDistributionDto
                {
                    Status = EnterpriseStatus.Active,
                    Count = summary?.ActiveEnterprises ?? 0
                },
                new PlatformStatusDistributionDto
                {
                    Status = EnterpriseStatus.Suspended,
                    Count = summary?.SuspendedEnterprises ?? 0
                },
                new PlatformStatusDistributionDto
                {
                    Status = EnterpriseStatus.Locked,
                    Count = summary?.LockedEnterprises ?? 0
                },
                new PlatformStatusDistributionDto
                {
                    Status = EnterpriseStatus.Inactive,
                    Count = summary?.InactiveEnterprises ?? 0
                }
            ],
            SubscriptionMix =
            [
                new PlatformSubscriptionMixDto
                {
                    TierName = EnterprisePlanTier.Free,
                    Count = freeCount
                },
                new PlatformSubscriptionMixDto
                {
                    TierName = EnterprisePlanTier.Pro,
                    Count = proCount
                }
            ],
            TopEnterprises = topEnterprises,
            ChurnWatchlist = churnCandidates
                .Select(enterprise => new ChurnWatchlistItemDto
                {
                    EnterpriseId = enterprise.Id,
                    EnterpriseName = enterprise.EnterpriseName,
                    Status = enterprise.Status,
                    RiskReason = GetRiskReason(enterprise.Status, enterprise.SubscriptionEndDate, now, expiringThreshold)!,
                    SubscriptionEndDate = enterprise.SubscriptionEndDate
                })
                .ToList(),
            IntegrationHealth = integrationHealth
        };
    }

    private static IntegrationHealthDto ComputeIntegrationHealth(GetSystemIntegrationsResponse integrations)
    {
        var health = new IntegrationHealthDto();

        foreach (var integration in integrations)
        {
            switch (integration.Status)
            {
                case "Configured":
                    health.Healthy++;
                    break;
                case "MissingConfiguration":
                    health.Warning++;
                    break;
                default:
                    health.Error++;
                    break;
            }
        }

        return health;
    }

    private static string? GetRiskReason(string status, DateTime subscriptionEndDate, DateTime now, DateTime expiringThreshold)
    {
        return status switch
        {
            EnterpriseStatus.Locked => "Doanh nghiệp đang bị khóa",
            EnterpriseStatus.Inactive => "Doanh nghiệp đã ngừng hoạt động",
            EnterpriseStatus.Suspended => "Doanh nghiệp đang tạm dừng",
            EnterpriseStatus.Active when subscriptionEndDate < now => "Subscription đã hết hạn",
            EnterpriseStatus.Active when subscriptionEndDate <= expiringThreshold =>
                $"Subscription sắp hết hạn trong {Math.Max(0, (subscriptionEndDate.Date - now.Date).Days)} ngày",
            _ => null
        };
    }

    private static Expression<Func<Enterprise, bool>> BuildIsProTierExpression()
    {
        var parameter = Expression.Parameter(typeof(Enterprise), "enterprise");
        var subscriptionPlan = Expression.Property(parameter, nameof(Enterprise.SubscriptionPlan));
        var planName = Expression.Property(subscriptionPlan, nameof(SubscriptionPlan.PlanName));
        var planCode = Expression.Property(subscriptionPlan, nameof(SubscriptionPlan.PlanCode));
        var emptyString = Expression.Constant(string.Empty);
        var toLowerMethod = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;
        var containsMethod = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;

        Expression? combined = null;

        foreach (var token in EnterprisePlanTier.ProPlanTokens)
        {
            var tokenConstant = Expression.Constant(token);

            var nameCoalesced = Expression.Call(Expression.Coalesce(planName, emptyString), toLowerMethod);
            var nameContains = Expression.Call(nameCoalesced, containsMethod, tokenConstant);

            var codeCoalesced = Expression.Call(Expression.Coalesce(planCode, emptyString), toLowerMethod);
            var codeContains = Expression.Call(codeCoalesced, containsMethod, tokenConstant);

            var tokenMatch = Expression.OrElse(nameContains, codeContains);
            combined = combined is null ? tokenMatch : Expression.OrElse(combined, tokenMatch);
        }

        return Expression.Lambda<Func<Enterprise, bool>>(combined!, parameter);
    }
}
