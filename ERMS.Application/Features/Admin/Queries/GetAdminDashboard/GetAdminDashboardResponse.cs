namespace ERMS.Application.Features.Admin.Queries.GetAdminDashboard;

public sealed class GetAdminDashboardResponse
{
    public int TotalEnterprises { get; set; }
    public int ActiveEnterprises { get; set; }
    public int LockedEnterprises { get; set; }
    public int ExpiringSoonEnterprises { get; set; }
    public List<AdminDashboardAttentionItemDto> AttentionItems { get; set; } = [];
    public List<AdminDashboardActivityDto> RecentActivities { get; set; } = [];
    public List<RecentPaymentDto> RecentPayments { get; set; } = [];
}

public sealed class AdminDashboardAttentionItemDto
{
    public Guid EnterpriseId { get; set; }
    public string EnterpriseName { get; set; } = string.Empty;
    public string EnterpriseCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string AttentionReason { get; set; } = string.Empty;
    public DateTime SubscriptionEndDate { get; set; }
}

public sealed class AdminDashboardActivityDto
{
    public Guid ApprovalHistoryId { get; set; }
    public Guid EnterpriseId { get; set; }
    public string EnterpriseName { get; set; } = string.Empty;
    public string EnterpriseCode { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? PreviousStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public string ChangedByName { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
}

public sealed class RecentPaymentDto
{
    public Guid EnterpriseId { get; set; }
    public string EnterpriseName { get; set; } = string.Empty;
    public string EnterpriseCode { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
}
