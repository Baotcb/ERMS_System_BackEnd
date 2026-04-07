using MediatR;

namespace ERMS.Application.Features.Subscription.Queries.GetCurrentSubscription;

public record GetCurrentSubscriptionQuery : IRequest<CurrentSubscriptionDto>;

public class CurrentSubscriptionDto
{
    public Guid EnterpriseId { get; set; }
    public string EnterpriseName { get; set; } = string.Empty;
    public SubscriptionPlanInfo CurrentPlan { get; set; } = null!;
    public DateTime SubscriptionStartDate { get; set; }
    public DateTime SubscriptionEndDate { get; set; }
    public string? SubscriptionStatus { get; set; }
    public UsageInfo Usage { get; set; } = null!;
    public bool HasPendingPayment { get; set; }
}

public class SubscriptionPlanInfo
{
    public Guid Id { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string PlanCode { get; set; } = string.Empty;
    public int MaxJobPostings { get; set; }
    public int MaxCourses { get; set; }
    public decimal Price { get; set; }
    public string? Features { get; set; }
}

public class UsageInfo
{
    public int CurrentJobPostings { get; set; }
    public int CurrentCourses { get; set; }
}
