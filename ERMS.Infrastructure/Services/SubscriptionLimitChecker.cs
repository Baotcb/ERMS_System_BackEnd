using ERMS.Application.Interface;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Infrastructure.Services;

public class SubscriptionLimitChecker : ISubscriptionLimitChecker
{
    private readonly IERMSDbContext _context;

    public SubscriptionLimitChecker(IERMSDbContext context)
    {
        _context = context;
    }

    public async Task<SubscriptionLimitResult> CheckJobPostingLimitAsync(Guid enterpriseId, CancellationToken ct = default)
    {
        var enterprise = await _context.Enterprises
            .Include(e => e.SubscriptionPlan)
            .FirstOrDefaultAsync(e => e.Id == enterpriseId && !e.IsDeleted, ct);

        if (enterprise?.SubscriptionPlan == null)
        {
            return new SubscriptionLimitResult
            {
                IsAllowed = false,
                Message = "Enterprise or plan not found"
            };
        }

        var plan = enterprise.SubscriptionPlan;
        var isExpired = enterprise.SubscriptionEndDate != default && enterprise.SubscriptionEndDate < DateTime.UtcNow;

        var currentCount = await _context.JobPostings
            .CountAsync(j => j.EnterpriseId == enterpriseId && !j.IsDeleted, ct);

        var max = isExpired && plan.PriceMonthly > 0
            ? 2 // fallback to Free plan limit when Pro expired
            : plan.MaxJobPostings;

        return new SubscriptionLimitResult
        {
            IsAllowed = currentCount < max,
            CurrentCount = currentCount,
            MaxAllowed = max,
            PlanName = enterprise.SubscriptionPlan.PlanName,
            Message = currentCount >= max
                ? isExpired
                    ? $"Gói {plan.PlanName} đã hết hạn. Vui lòng gia hạn để tiếp tục sử dụng."
                    : $"Đã đạt giới hạn {max} tin tuyển dụng của gói {plan.PlanName}. Vui lòng nâng cấp gói dịch vụ."
                : null
        };
    }

    public async Task<SubscriptionLimitResult> CheckCourseLimitAsync(Guid enterpriseId, CancellationToken ct = default)
    {
        var enterprise = await _context.Enterprises
            .Include(e => e.SubscriptionPlan)
            .FirstOrDefaultAsync(e => e.Id == enterpriseId && !e.IsDeleted, ct);

        if (enterprise?.SubscriptionPlan == null)
        {
            return new SubscriptionLimitResult
            {
                IsAllowed = false,
                Message = "Enterprise or plan not found"
            };
        }

        var plan = enterprise.SubscriptionPlan;
        var isExpired = enterprise.SubscriptionEndDate != default && enterprise.SubscriptionEndDate < DateTime.UtcNow;

        var currentCount = await _context.Courses
            .CountAsync(c => c.EnterpriseId == enterpriseId && !c.IsDeleted, ct);

        var max = isExpired && plan.PriceMonthly > 0
            ? 2 // fallback to Free plan limit when Pro expired
            : plan.MaxCourses;

        return new SubscriptionLimitResult
        {
            IsAllowed = currentCount < max,
            CurrentCount = currentCount,
            MaxAllowed = max,
            PlanName = enterprise.SubscriptionPlan.PlanName,
            Message = currentCount >= max
                ? isExpired
                    ? $"Gói {plan.PlanName} đã hết hạn. Vui lòng gia hạn để tiếp tục sử dụng."
                    : $"Đã đạt giới hạn {max} khóa đào tạo của gói {plan.PlanName}. Vui lòng nâng cấp gói dịch vụ."
                : null
        };
    }

    public async Task<SubscriptionLimitResult> CheckEmployeeLimitAsync(Guid enterpriseId, CancellationToken ct = default)
    {
        var enterprise = await _context.Enterprises
            .Include(e => e.SubscriptionPlan)
            .FirstOrDefaultAsync(e => e.Id == enterpriseId && !e.IsDeleted, ct);

        if (enterprise?.SubscriptionPlan == null)
        {
            return new SubscriptionLimitResult
            {
                IsAllowed = false,
                Message = "Enterprise or plan not found"
            };
        }

        var plan = enterprise.SubscriptionPlan;
        var isExpired = enterprise.SubscriptionEndDate != default && enterprise.SubscriptionEndDate < DateTime.UtcNow;

        var currentCount = await _context.Employees
            .CountAsync(e => e.EnterpriseId == enterpriseId && !e.IsDeleted, ct);

        var max = isExpired && plan.PriceMonthly > 0
            ? 5 // fallback to Free plan limit when Pro expired
            : plan.MaxUsers;

        return new SubscriptionLimitResult
        {
            IsAllowed = currentCount < max,
            CurrentCount = currentCount,
            MaxAllowed = max,
            PlanName = enterprise.SubscriptionPlan.PlanName,
            Message = currentCount >= max
                ? isExpired
                    ? $"Gói {plan.PlanName} đã hết hạn. Vui lòng gia hạn để tiếp tục sử dụng."
                    : $"Đã đạt giới hạn {max} nhân viên của gói {plan.PlanName}. Vui lòng nâng cấp gói dịch vụ."
                : null
        };
    }

    public async Task<bool> IsProPlanAsync(Guid enterpriseId, CancellationToken ct = default)
    {
        var enterprise = await _context.Enterprises
            .Where(e => e.Id == enterpriseId && !e.IsDeleted)
            .Select(e => new { e.SubscriptionPlan.PlanCode, e.SubscriptionEndDate })
            .FirstOrDefaultAsync(ct);

        if (enterprise == null)
            return false;

        var isExpired = enterprise.SubscriptionEndDate != default && enterprise.SubscriptionEndDate < DateTime.UtcNow;
        if (isExpired)
            return false;

        return string.Equals(enterprise.PlanCode, "PRO", StringComparison.OrdinalIgnoreCase);
    }
}
