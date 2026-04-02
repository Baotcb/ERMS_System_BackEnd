using ERMS.Application.Features.Admin;
using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Admin.Queries.GetEnterpriseList;

    public sealed class GetEnterpriseListHandler : IRequestHandler<GetEnterpriseListQuery, GetEnterpriseListResponse>
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 100;

    private readonly IERMSDbContext _context;

    public GetEnterpriseListHandler(IERMSDbContext context)
    {
        _context = context;
    }

    public async Task<GetEnterpriseListResponse> Handle(GetEnterpriseListQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var enterprisesQuery = _context.Enterprises
            .AsNoTracking()
            .Where(enterprise => !enterprise.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            enterprisesQuery = enterprisesQuery.Where(enterprise =>
                enterprise.EnterpriseName.ToLower().Contains(search) ||
                enterprise.EnterpriseCode.ToLower().Contains(search) ||
                (enterprise.Email != null && enterprise.Email.ToLower().Contains(search)) ||
                (enterprise.Phone != null && enterprise.Phone.ToLower().Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            enterprisesQuery = enterprisesQuery.Where(enterprise => enterprise.Status == request.Status);
        }

        if (!string.IsNullOrWhiteSpace(request.PlanTier))
        {
            enterprisesQuery = EnterprisePlanTier.ApplyTierFilter(enterprisesQuery, request.PlanTier);
        }
        else if (!string.IsNullOrWhiteSpace(request.PlanCode))
        {
            enterprisesQuery = enterprisesQuery.Where(enterprise => enterprise.SubscriptionPlan.PlanCode == request.PlanCode);
        }

        if (request.ExpiringWithinDays.HasValue && request.ExpiringWithinDays.Value > 0)
        {
            var expiringThreshold = now.AddDays(request.ExpiringWithinDays.Value);
            enterprisesQuery = enterprisesQuery.Where(enterprise =>
                enterprise.SubscriptionEndDate >= now &&
                enterprise.SubscriptionEndDate <= expiringThreshold);
        }

        var totalCount = await enterprisesQuery.CountAsync(cancellationToken);

        var pageNumber = Math.Max(request.PageNumber, 1);
        var pageSize = Math.Clamp(request.PageSize < 1 ? DefaultPageSize : request.PageSize, 1, MaxPageSize);

        var enterprises = await enterprisesQuery
            .OrderByDescending(enterprise => enterprise.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(enterprise => new
            {
                enterprise.Id,
                enterprise.EnterpriseName,
                enterprise.EnterpriseCode,
                enterprise.Status,
                enterprise.Email,
                enterprise.Phone,
                CurrentPlanName = enterprise.SubscriptionPlan.PlanName,
                CurrentPlanCode = enterprise.SubscriptionPlan.PlanCode,
                enterprise.SubscriptionEndDate,
                enterprise.CreatedAt,
                enterprise.LogoUrl
            })
            .ToListAsync(cancellationToken);

        var enterpriseIds = enterprises.Select(enterprise => enterprise.Id).ToList();
        var employeeCounts = enterpriseIds.Count == 0
            ? new Dictionary<Guid, int>()
            : await _context.Employees
                .AsNoTracking()
                .Where(employee => enterpriseIds.Contains(employee.EnterpriseId) && !employee.IsDeleted)
                .GroupBy(employee => employee.EnterpriseId)
                .ToDictionaryAsync(group => group.Key, group => group.Count(), cancellationToken);

        return new GetEnterpriseListResponse
        {
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize),
            Items = enterprises.Select(enterprise => new EnterpriseListItemDto
            {
                EnterpriseId = enterprise.Id,
                EnterpriseName = enterprise.EnterpriseName,
                EnterpriseCode = enterprise.EnterpriseCode,
                Status = enterprise.Status,
                ContactEmail = enterprise.Email,
                ContactPhone = enterprise.Phone,
                CurrentPlanName = enterprise.CurrentPlanName,
                CurrentPlanCode = enterprise.CurrentPlanCode,
                SubscriptionEndDate = enterprise.SubscriptionEndDate,
                CreatedAt = enterprise.CreatedAt,
                EmployeeCount = employeeCounts.TryGetValue(enterprise.Id, out var employeeCount) ? employeeCount : 0,
                LogoUrl = enterprise.LogoUrl
            }).ToList()
        };
    }
}
