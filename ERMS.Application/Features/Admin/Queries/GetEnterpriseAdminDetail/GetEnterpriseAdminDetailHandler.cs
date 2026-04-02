using ERMS.Application.Interface;
using ERMS.Domain.Constants.Enterprise;
using ERMS.Domain.Entities.Enterprise;
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
        var subscriptionHistorySet = _context.SubscriptionHistories;
        var subscriptionHistories = subscriptionHistorySet is null
            ? Enumerable.Empty<SubscriptionHistory>().AsQueryable()
            : subscriptionHistorySet.AsNoTracking();
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
                SubscriptionStatus = enterprise.SubscriptionStatus,
                DepartmentCount = _context.Departments
                    .AsNoTracking()
                    .Count(department => department.EnterpriseId == enterprise.Id && !department.IsDeleted),
                EmployeeCount = _context.Employees
                    .AsNoTracking()
                    .Count(employee => employee.EnterpriseId == enterprise.Id && !employee.IsDeleted),
                JobPostingCount = _context.JobPostings
                    .AsNoTracking()
                    .Count(jobPosting => jobPosting.EnterpriseId == enterprise.Id && !jobPosting.IsDeleted),
                CourseCount = _context.Courses
                    .AsNoTracking()
                    .Count(course => course.EnterpriseId == enterprise.Id && !course.IsDeleted),
                RecentPayment = subscriptionHistories
                    .Where(history => history.EnterpriseId == enterprise.Id)
                    .OrderByDescending(history => history.CreatedAt)
                    .Select(history => new RecentPaymentSummary
                    {
                        Amount = history.Amount,
                        PaymentMethod = history.PaymentMethod,
                        PaymentReference = history.PaymentReference,
                        PaidAt = history.CreatedAt
                    })
                    .FirstOrDefault(),
                TotalSpent = subscriptionHistories
                    .Where(history => history.EnterpriseId == enterprise.Id)
                    .Sum(history => (decimal?)history.Amount) ?? 0
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (response is null)
        {
            throw new KeyNotFoundException($"Không tìm thấy doanh nghiệp với ID {request.EnterpriseId}.");
        }

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
            riskFlags.Add("Doanh nghiệp đang bị khóa.");
        }
        else if (status == EnterpriseStatus.Suspended)
        {
            riskFlags.Add("Doanh nghiệp đang tạm dừng.");
        }
        else if (status == EnterpriseStatus.Inactive)
        {
            riskFlags.Add("Doanh nghiệp đã ngừng hoạt động.");
        }

        if (subscriptionEndDate < now)
        {
            riskFlags.Add("Subscription đã hết hạn.");
        }
        else if (subscriptionEndDate <= now.AddDays(30))
        {
            var daysRemaining = Math.Max(0, (subscriptionEndDate.Date - now.Date).Days);
            riskFlags.Add($"Subscription sắp hết hạn trong {daysRemaining} ngày.");
        }

        if (!hasPaymentHistory)
        {
            riskFlags.Add("Chưa có lịch sử thanh toán.");
        }

        return riskFlags;
    }
}
