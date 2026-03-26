using ERMS.Application.Interface;
using ERMS.Domain.Constants.Enterprise;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Admin.Queries.GetAdminDashboard;

public sealed class GetAdminDashboardHandler : IRequestHandler<GetAdminDashboardQuery, GetAdminDashboardResponse>
{
    private readonly IERMSDbContext _context;

    public GetAdminDashboardHandler(IERMSDbContext context)
    {
        _context = context;
    }

    public async Task<GetAdminDashboardResponse> Handle(GetAdminDashboardQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var expiringThreshold = now.AddDays(30);
        var enterprises = _context.Enterprises
            .AsNoTracking()
            .Where(enterprise => !enterprise.IsDeleted);
        var enterpriseSnapshots = await enterprises
            .Select(enterprise => new
            {
                enterprise.Id,
                enterprise.EnterpriseName,
                enterprise.EnterpriseCode,
                enterprise.Status,
                enterprise.SubscriptionEndDate
            })
            .ToListAsync(cancellationToken);

        return new GetAdminDashboardResponse
        {
            TotalEnterprises = enterpriseSnapshots.Count,
            ActiveEnterprises = enterpriseSnapshots.Count(enterprise => enterprise.Status == EnterpriseStatus.Active),
            LockedEnterprises = enterpriseSnapshots.Count(enterprise => enterprise.Status == EnterpriseStatus.Locked),
            ExpiringSoonEnterprises = enterpriseSnapshots.Count(enterprise =>
                enterprise.Status == EnterpriseStatus.Active &&
                enterprise.SubscriptionEndDate >= now &&
                enterprise.SubscriptionEndDate <= expiringThreshold),
            AttentionItems = enterpriseSnapshots
                .Select(enterprise => new
                {
                    enterprise.Id,
                    enterprise.EnterpriseName,
                    enterprise.EnterpriseCode,
                    enterprise.Status,
                    enterprise.SubscriptionEndDate,
                    Priority = GetAttentionPriority(enterprise.Status, enterprise.SubscriptionEndDate, now, expiringThreshold),
                    AttentionReason = GetAttentionReason(enterprise.Status, enterprise.SubscriptionEndDate, now, expiringThreshold)
                })
                .Where(item => item.Priority.HasValue && item.AttentionReason != null)
                .OrderBy(item => item.Priority)
                .ThenBy(item => item.SubscriptionEndDate)
                .Take(6)
                .Select(item => new AdminDashboardAttentionItemDto
                {
                    EnterpriseId = item.Id,
                    EnterpriseName = item.EnterpriseName,
                    EnterpriseCode = item.EnterpriseCode,
                    Status = item.Status,
                    AttentionReason = item.AttentionReason!,
                    SubscriptionEndDate = item.SubscriptionEndDate
                })
                .ToList(),
            RecentActivities = await _context.ApprovalHistories
                .AsNoTracking()
                .Where(history => history.EntityType == "Enterprise")
                .OrderByDescending(history => history.CreatedAt)
                .Take(6)
                .Select(history => new AdminDashboardActivityDto
                {
                    ApprovalHistoryId = history.Id,
                    EnterpriseId = history.EntityId,
                    EnterpriseName = _context.Enterprises
                        .Where(enterprise => enterprise.Id == history.EntityId)
                        .Select(enterprise => enterprise.EnterpriseName)
                        .FirstOrDefault() ?? string.Empty,
                    EnterpriseCode = _context.Enterprises
                        .Where(enterprise => enterprise.Id == history.EntityId)
                        .Select(enterprise => enterprise.EnterpriseCode)
                        .FirstOrDefault() ?? string.Empty,
                    Action = history.Action,
                    PreviousStatus = history.PreviousStatus,
                    NewStatus = history.NewStatus,
                    ChangedByName = history.PerformedBy == null ? string.Empty : history.PerformedBy.FullName,
                    ChangedAt = history.CreatedAt
                })
                .ToListAsync(cancellationToken),
            RecentPayments = await _context.SubscriptionHistories
                .AsNoTracking()
                .OrderByDescending(history => history.CreatedAt)
                .Take(5)
                .Select(history => new RecentPaymentDto
                {
                    EnterpriseId = history.EnterpriseId,
                    EnterpriseName = history.Enterprise.EnterpriseName,
                    EnterpriseCode = history.Enterprise.EnterpriseCode,
                    ActionType = history.ActionType,
                    Amount = history.Amount,
                    CreatedAt = history.CreatedAt
                })
                .ToListAsync(cancellationToken)
        };
    }

    private static int? GetAttentionPriority(string status, DateTime subscriptionEndDate, DateTime now, DateTime expiringThreshold)
    {
        return status switch
        {
            EnterpriseStatus.Locked => 0,
            EnterpriseStatus.Suspended => 1,
            EnterpriseStatus.Inactive => 2,
            EnterpriseStatus.Active when subscriptionEndDate < now => 3,
            EnterpriseStatus.Active when subscriptionEndDate <= expiringThreshold => 4,
            _ => null
        };
    }

    private static string? GetAttentionReason(string status, DateTime subscriptionEndDate, DateTime now, DateTime expiringThreshold)
    {
        return status switch
        {
            EnterpriseStatus.Locked => "Doanh nghiep dang bi khoa",
            EnterpriseStatus.Suspended => "Doanh nghiep dang tam dung",
            EnterpriseStatus.Inactive => "Doanh nghiep da ngung hoat dong",
            EnterpriseStatus.Active when subscriptionEndDate < now => "Subscription da het han",
            EnterpriseStatus.Active when subscriptionEndDate <= expiringThreshold => "Subscription sap het han",
            _ => null
        };
    }
}
