using ERMS.Application.Interface;
using ERMS.Domain.Constants.Enterprise;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Subscription.Queries.GetCurrentSubscription;

public class GetCurrentSubscriptionHandler : IRequestHandler<GetCurrentSubscriptionQuery, CurrentSubscriptionDto>
{
    private static readonly TimeSpan PendingOrderTtl = TimeSpan.FromMinutes(30);

    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetCurrentSubscriptionHandler(IERMSDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<CurrentSubscriptionDto> Handle(GetCurrentSubscriptionQuery request, CancellationToken cancellationToken)
    {
        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync()
            ?? throw new UnauthorizedAccessException("Người dùng không liên kết với doanh nghiệp nào");

        var enterprise = await _context.Enterprises
            .AsNoTracking()
            .Include(e => e.SubscriptionPlan)
            .FirstOrDefaultAsync(e => e.Id == enterpriseId && !e.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy doanh nghiệp");

        var currentJobPostings = await _context.JobPostings
            .AsNoTracking()
            .CountAsync(j => j.EnterpriseId == enterpriseId && !j.IsDeleted, cancellationToken);

        var currentCourses = await _context.Courses
            .AsNoTracking()
            .CountAsync(c => c.EnterpriseId == enterpriseId && !c.IsDeleted, cancellationToken);

        var pendingCutoff = DateTime.UtcNow.Subtract(PendingOrderTtl);
        var hasPendingPayment = await _context.PaymentOrders
            .AsNoTracking()
            .AnyAsync(p =>
                p.EnterpriseId == enterpriseId
                && p.Status == PaymentOrderConstants.Status.Pending
                && p.CreatedAt >= pendingCutoff,
                cancellationToken);

        return new CurrentSubscriptionDto
        {
            EnterpriseId = enterprise.Id,
            EnterpriseName = enterprise.EnterpriseName,
            CurrentPlan = new SubscriptionPlanInfo
            {
                Id = enterprise.SubscriptionPlan.Id,
                PlanName = enterprise.SubscriptionPlan.PlanName,
                PlanCode = enterprise.SubscriptionPlan.PlanCode,
                MaxJobPostings = enterprise.SubscriptionPlan.MaxJobPostings,
                MaxCourses = enterprise.SubscriptionPlan.MaxCourses,
                Price = enterprise.SubscriptionPlan.PriceMonthly,
                Features = enterprise.SubscriptionPlan.Features
            },
            SubscriptionStartDate = enterprise.SubscriptionStartDate,
            SubscriptionEndDate = enterprise.SubscriptionEndDate,
            SubscriptionStatus = enterprise.SubscriptionStatus,
            Usage = new UsageInfo
            {
                CurrentJobPostings = currentJobPostings,
                CurrentCourses = currentCourses
            },
            HasPendingPayment = hasPendingPayment
        };
    }
}
