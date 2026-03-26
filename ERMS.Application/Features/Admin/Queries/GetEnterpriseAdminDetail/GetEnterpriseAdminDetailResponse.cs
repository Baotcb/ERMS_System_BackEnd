namespace ERMS.Application.Features.Admin.Queries.GetEnterpriseAdminDetail;

public sealed class GetEnterpriseAdminDetailResponse
{
    public Guid EnterpriseId { get; set; }
    public string EnterpriseName { get; set; } = string.Empty;
    public string EnterpriseCode { get; set; } = string.Empty;
    public string? TaxCode { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? LogoUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedByName { get; set; }
    public string Status { get; set; } = string.Empty;
    public EnterprisePlanSummaryDto CurrentPlan { get; set; } = new();
    public DateTime SubscriptionStartDate { get; set; }
    public DateTime SubscriptionEndDate { get; set; }
    public string SubscriptionStatus { get; set; } = string.Empty;
    public int DepartmentCount { get; set; }
    public int EmployeeCount { get; set; }
    public int JobPostingCount { get; set; }
    public int CourseCount { get; set; }
    public decimal TotalSpent { get; set; }
    public RecentPaymentSummary? RecentPayment { get; set; }
    public List<string> RiskFlags { get; set; } = [];
    public List<AdminStatusHistoryDto> StatusHistory { get; set; } = [];
}

public sealed class EnterprisePlanSummaryDto
{
    public Guid PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string PlanCode { get; set; } = string.Empty;
    public decimal PriceMonthly { get; set; }
    public decimal PriceYearly { get; set; }
    public int MaxUsers { get; set; }
    public int MaxJobPostings { get; set; }
    public int MaxCourses { get; set; }
}

public sealed class RecentPaymentSummary
{
    public decimal Amount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentReference { get; set; }
    public DateTime PaidAt { get; set; }
}

public sealed class AdminStatusHistoryDto
{
    public Guid ApprovalHistoryId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? PreviousStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public string? AdminNote { get; set; }
    public string ChangedByName { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
}
