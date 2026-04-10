using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Subscription.Queries.GetSubscriptionPlans;

public class GetSubscriptionPlansHandler : IRequestHandler<GetSubscriptionPlansQuery, List<SubscriptionPlanDto>>
{
    private readonly IERMSDbContext _context;

    public GetSubscriptionPlansHandler(IERMSDbContext context)
    {
        _context = context;
    }

    public async Task<List<SubscriptionPlanDto>> Handle(GetSubscriptionPlansQuery request, CancellationToken cancellationToken)
    {
        return await _context.SubscriptionPlans
            .AsNoTracking()
            .Where(x => x.IsActive && !x.IsDeleted)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new SubscriptionPlanDto
            {
                Id = x.Id,
                PlanName = x.PlanName,
                PlanCode = x.PlanCode,
                Description = x.Description,
                MaxJobPostings = x.MaxJobPostings,
                MaxCourses = x.MaxCourses,
                Price = x.PriceMonthly,
                Features = x.Features,
                DisplayOrder = x.DisplayOrder
            })
            .ToListAsync(cancellationToken);
    }
}
