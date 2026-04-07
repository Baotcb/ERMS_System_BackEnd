using MediatR;

namespace ERMS.Application.Features.Subscription.Queries.GetSubscriptionPlans;

public record GetSubscriptionPlansQuery : IRequest<List<SubscriptionPlanDto>>;

public class SubscriptionPlanDto
{
    public Guid Id { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string PlanCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int MaxJobPostings { get; set; }
    public int MaxCourses { get; set; }
    public decimal Price { get; set; }
    public string? Features { get; set; }
    public int DisplayOrder { get; set; }
}
