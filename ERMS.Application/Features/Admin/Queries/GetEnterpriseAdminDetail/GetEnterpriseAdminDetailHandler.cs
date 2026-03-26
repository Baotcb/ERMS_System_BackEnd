using ERMS.Application.Interface;
using ERMS.Domain.Constants.Enterprise;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Admin.Queries.GetEnterpriseAdminDetail;

public sealed class GetEnterpriseAdminDetailHandler : IRequestHandler<GetEnterpriseAdminDetailQuery, GetEnterpriseAdminDetailResponse>
{
    private readonly IERMSDbContext _context;

    public GetEnterpriseAdminDetailHandler(IERMSDbContext context)
    {
        _context = context;
    }

    public async Task<GetEnterpriseAdminDetailResponse> Handle(GetEnterpriseAdminDetailQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var response = await _context.Enterprises
            .AsNoTracking()
            .Where(enterprise => enterprise.Id == request.EnterpriseId && !enterprise.IsDeleted)
            .Select(enterprise => new GetEnterpriseAdminDetailResponse
            {
                EnterpriseId = enterprise.Id,
                EnterpriseName = enterprise.EnterpriseName,
                EnterpriseCode = enterprise.EnterpriseCode,
                TaxCode = enterprise.TaxCode,
                Address = enterprise.Address,
                Phone = enterprise.Phone,
                Email = enterprise.Email,
                Website = enterprise.Website,
                LogoUrl = enterprise.LogoUrl,
                CreatedAt = enterprise.CreatedAt,
                CreatedByName = enterprise.CreatedBy != null ? enterprise.CreatedBy.FullName : null,
                Status = enterprise.Status,
                CurrentPlan = new EnterprisePlanSummaryDto
                {
                    PlanId = enterprise.SubscriptionPlan.Id,
                    PlanName = enterprise.SubscriptionPlan.PlanName,
                    PlanCode = enterprise.SubscriptionPlan.PlanCode,
                    PriceMonthly = enterprise.SubscriptionPlan.PriceMonthly,
                    PriceYearly = enterprise.SubscriptionPlan.PriceYearly,
                    MaxUsers = enterprise.SubscriptionPlan.MaxUsers,
                    MaxJobPostings = enterprise.SubscriptionPlan.MaxJobPostings,
                    MaxCourses = enterprise.SubscriptionPlan.MaxCourses
                },
                SubscriptionStartDate = enterprise.SubscriptionStartDate,
                SubscriptionEndDate = enterprise.SubscriptionEndDate,
                SubscriptionStatus = enterprise.SubscriptionStatus
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (response is null)
        {
            throw new KeyNotFoundException($"Không tìm thấy doanh nghiệp với ID {request.EnterpriseId}.");
        }

        response.DepartmentCount = await _context.Departments
            .AsNoTracking()
            .CountAsync(department => department.EnterpriseId == request.EnterpriseId && !department.IsDeleted, cancellationToken);

        response.EmployeeCount = await _context.Employees
            .AsNoTracking()
            .CountAsync(employee => employee.EnterpriseId == request.EnterpriseId && !employee.IsDeleted, cancellationToken);

        response.JobPostingCount = await _context.JobPostings
            .AsNoTracking()
            .CountAsync(jobPosting => jobPosting.EnterpriseId == request.EnterpriseId && !jobPosting.IsDeleted, cancellationToken);

        response.CourseCount = await _context.Courses
            .AsNoTracking()
            .CountAsync(course => course.EnterpriseId == request.EnterpriseId && !course.IsDeleted, cancellationToken);

        response.RecentPayment = await _context.SubscriptionHistories
            .AsNoTracking()
            .Where(history => history.EnterpriseId == request.EnterpriseId)
            .OrderByDescending(history => history.CreatedAt)
            .Select(history => new RecentPaymentSummary
            {
                Amount = history.Amount,
                PaymentMethod = history.PaymentMethod,
                PaymentReference = history.PaymentReference,
                PaidAt = history.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        response.TotalSpent = await _context.SubscriptionHistories
            .AsNoTracking()
            .Where(history => history.EnterpriseId == request.EnterpriseId)
            .SumAsync(history => (decimal?)history.Amount, cancellationToken) ?? 0;

        response.StatusHistory = await _context.ApprovalHistories
            .AsNoTracking()
            .Where(history => history.EntityType == "Enterprise" && history.EntityId == request.EnterpriseId)
            .OrderByDescending(history => history.CreatedAt)
            .Select(history => new AdminStatusHistoryDto
            {
                ApprovalHistoryId = history.Id,
                Action = history.Action,
                PreviousStatus = history.PreviousStatus,
                NewStatus = history.NewStatus,
                AdminNote = history.Note,
                ChangedByName = history.PerformedBy != null ? history.PerformedBy.FullName : string.Empty,
                ChangedAt = history.CreatedAt
            })
            .ToListAsync(cancellationToken);

        response.RiskFlags = BuildRiskFlags(response.Status, response.SubscriptionEndDate, response.RecentPayment != null, now);

        return response;
    }

    private static List<string> BuildRiskFlags(string status, DateTime subscriptionEndDate, bool hasPaymentHistory, DateTime now)
    {
        var riskFlags = new List<string>();

        if (status == EnterpriseStatus.Locked)
        {
            riskFlags.Add("Doanh nghiep dang bi khoa.");
        }
        else if (status == EnterpriseStatus.Suspended)
        {
            riskFlags.Add("Doanh nghiep dang tam dung.");
        }
        else if (status == EnterpriseStatus.Inactive)
        {
            riskFlags.Add("Doanh nghiep da ngung hoat dong.");
        }

        if (subscriptionEndDate < now)
        {
            riskFlags.Add("Subscription da het han.");
        }
        else if (subscriptionEndDate <= now.AddDays(30))
        {
            var daysRemaining = Math.Max(0, (subscriptionEndDate.Date - now.Date).Days);
            riskFlags.Add($"Subscription sap het han trong {daysRemaining} ngay.");
        }

        if (!hasPaymentHistory)
        {
            riskFlags.Add("Chua co lich su thanh toan.");
        }

        return riskFlags;
    }
}
