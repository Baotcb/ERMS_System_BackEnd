namespace ERMS.Application.Interface;

public class SubscriptionLimitResult
{
    public bool IsAllowed { get; set; }
    public int CurrentCount { get; set; }
    public int MaxAllowed { get; set; }
    public string? PlanName { get; set; }
    public string? Message { get; set; }
}

public interface ISubscriptionLimitChecker
{
    Task<SubscriptionLimitResult> CheckJobPostingLimitAsync(Guid enterpriseId, CancellationToken ct = default);
    Task<SubscriptionLimitResult> CheckCourseLimitAsync(Guid enterpriseId, CancellationToken ct = default);
    Task<SubscriptionLimitResult> CheckEmployeeLimitAsync(Guid enterpriseId, CancellationToken ct = default);
    Task<bool> IsProPlanAsync(Guid enterpriseId, CancellationToken ct = default);
}
