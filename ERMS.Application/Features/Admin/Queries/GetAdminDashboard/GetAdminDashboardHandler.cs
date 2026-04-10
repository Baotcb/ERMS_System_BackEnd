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

        var summary = await enterprises
            .GroupBy(_ => 1)
            .Select(group => new
            {
                TotalEnterprises = group.Count(),
                ActiveEnterprises = group.Count(enterprise => enterprise.Status == EnterpriseStatus.Active),
                LockedEnterprises = group.Count(enterprise => enterprise.Status == EnterpriseStatus.Locked),
                ExpiringSoonEnterprises = group.Count(enterprise =>
                    enterprise.Status == EnterpriseStatus.Active &&
                    enterprise.SubscriptionEndDate >= now &&
                    enterprise.SubscriptionEndDate <= expiringThreshold)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var attentionItems = await enterprises
            .Where(enterprise =>
                enterprise.Status == EnterpriseStatus.Locked ||
                enterprise.Status == EnterpriseStatus.Suspended ||
                enterprise.Status == EnterpriseStatus.Inactive ||
                (enterprise.Status == EnterpriseStatus.Active && enterprise.SubscriptionEndDate <= expiringThreshold))
            .Select(enterprise => new
            {
                enterprise.Id,
                enterprise.EnterpriseName,
                enterprise.EnterpriseCode,
                enterprise.Status,
                enterprise.SubscriptionEndDate,
                Priority = enterprise.Status == EnterpriseStatus.Locked
                    ? 0
                    : enterprise.Status == EnterpriseStatus.Suspended
                        ? 1
                        : enterprise.Status == EnterpriseStatus.Inactive
                            ? 2
                            : enterprise.Status == EnterpriseStatus.Active && enterprise.SubscriptionEndDate < now
                                ? 3
                                : enterprise.Status == EnterpriseStatus.Active && enterprise.SubscriptionEndDate <= expiringThreshold
                                    ? 4
                                    : (int?)null,
                AttentionReason = enterprise.Status == EnterpriseStatus.Locked
                    ? "Doanh nghiệp đang bị khóa"
                    : enterprise.Status == EnterpriseStatus.Suspended
                        ? "Doanh nghiệp đang tạm dừng"
                        : enterprise.Status == EnterpriseStatus.Inactive
                            ? "Doanh nghiệp đã ngừng hoạt động"
                            : enterprise.Status == EnterpriseStatus.Active && enterprise.SubscriptionEndDate < now
                                ? "Subscription đã hết hạn"
                                : enterprise.Status == EnterpriseStatus.Active && enterprise.SubscriptionEndDate <= expiringThreshold
                                    ? "Subscription sắp hết hạn"
                                    : null
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
            .ToListAsync(cancellationToken);

        var recentActivities = await (
            from history in _context.ApprovalHistories.AsNoTracking()
            where history.EntityType == "Enterprise"
            join enterprise in _context.Enterprises
                .AsNoTracking()
                .Where(value => !value.IsDeleted)
                on history.EntityId equals enterprise.Id into enterpriseGroup
            from enterprise in enterpriseGroup.DefaultIfEmpty()
            orderby history.CreatedAt descending
            select new AdminDashboardActivityDto
            {
                ApprovalHistoryId = history.Id,
                EnterpriseId = history.EntityId,
                EnterpriseName = enterprise != null ? enterprise.EnterpriseName : string.Empty,
                EnterpriseCode = enterprise != null ? enterprise.EnterpriseCode : string.Empty,
                Action = history.Action,
                PreviousStatus = history.PreviousStatus,
                NewStatus = history.NewStatus,
                ChangedByName = history.PerformedBy != null ? history.PerformedBy.FullName : string.Empty,
                ChangedAt = history.CreatedAt
            })
            .Take(6)
            .ToListAsync(cancellationToken);

        var recentPayments = await _context.SubscriptionHistories
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
            .ToListAsync(cancellationToken);

        return new GetAdminDashboardResponse
        {
            TotalEnterprises = summary?.TotalEnterprises ?? 0,
            ActiveEnterprises = summary?.ActiveEnterprises ?? 0,
            LockedEnterprises = summary?.LockedEnterprises ?? 0,
            ExpiringSoonEnterprises = summary?.ExpiringSoonEnterprises ?? 0,
            AttentionItems = attentionItems,
            RecentActivities = recentActivities,
            RecentPayments = recentPayments
        };
    }
}
