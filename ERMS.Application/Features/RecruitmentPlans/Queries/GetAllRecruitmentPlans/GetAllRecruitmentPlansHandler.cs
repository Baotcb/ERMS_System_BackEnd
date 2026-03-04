using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.RecruitmentPlans.Queries.GetAllRecruitmentPlans;

public sealed class GetAllRecruitmentPlansHandler : IRequestHandler<GetAllRecruitmentPlansQuery, GetAllRecruitmentPlansResult>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetAllRecruitmentPlansHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<GetAllRecruitmentPlansResult> Handle(GetAllRecruitmentPlansQuery request, CancellationToken cancellationToken)
    {
        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();
        if (enterpriseId == null)
        {
            throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào");
        }

        var query = _context.RecruitmentPlans
            .Where(rp => rp.EnterpriseId == enterpriseId.Value && !rp.IsDeleted)
            .AsQueryable();

        // 2a. Department isolation: limit query to User's Department if user is DepartmentHead
        var userRoles = _currentUserService.Roles;
        if (userRoles != null && userRoles.Contains(AppRoles.DepartmentHead))
        {
            var userDepartmentId = await _currentUserService.GetDepartmentIdAsync();
            if (userDepartmentId == null)
            {
                throw new UnauthorizedAccessException("Người dùng không thuộc phòng ban nào");
            }
            query = query.Where(rp => rp.DepartmentId == userDepartmentId.Value);
        }

        // Search filter
        if (!string.IsNullOrEmpty(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(rp =>
                rp.PlanName.ToLower().Contains(search) ||
                rp.PlanCode.ToLower().Contains(search));
        }

        // Status filter
        if (!string.IsNullOrEmpty(request.Status))
        {
            query = query.Where(rp => rp.Status == request.Status);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(rp => rp.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(rp => new RecruitmentPlanDto
            {
                Id = rp.Id,
                CampaignId = rp.CampaignId,
                CampaignName = rp.Campaign.CampaignName,
                PlanName = rp.PlanName,
                PlanCode = rp.PlanCode,
                Description = rp.Description,
                StartDate = rp.StartDate,
                EndDate = rp.EndDate,
                TotalBudget = rp.TotalBudget,
                Status = rp.Status,
                CreatedByName = rp.CreatedBy.FullName,
                ApprovedByName = rp.ApprovedBy != null ? rp.ApprovedBy.FullName : null,
                ApprovedAt = rp.ApprovedAt,
                CreatedAt = rp.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new GetAllRecruitmentPlansResult
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
