using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.RecruitmentPlans.Queries.GetRecruitmentPlanById;

public sealed class GetRecruitmentPlanByIdHandler : IRequestHandler<GetRecruitmentPlanByIdQuery, RecruitmentPlanDetailDto>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetRecruitmentPlanByIdHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<RecruitmentPlanDetailDto> Handle(GetRecruitmentPlanByIdQuery request, CancellationToken cancellationToken)
    {
        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();
        if (enterpriseId == null)
        {
            throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào");
        }

        var recruitmentPlan = await _context.RecruitmentPlans
            .Where(rp => rp.Id == request.Id
                      && rp.EnterpriseId == enterpriseId.Value
                      && !rp.IsDeleted)
            .Select(rp => new RecruitmentPlanDetailDto
            {
                Id = rp.Id,
                EnterpriseId = rp.EnterpriseId,
                CampaignId = rp.CampaignId,
                CampaignName = rp.Campaign.CampaignName,
                PlanName = rp.PlanName,
                PlanCode = rp.PlanCode,
                Description = rp.Description,
                StartDate = rp.StartDate,
                EndDate = rp.EndDate,
                TotalBudget = rp.TotalBudget,
                Status = rp.Status,
                CreatedById = rp.CreatedById,
                CreatedByName = rp.CreatedBy.FullName,
                ApprovedById = rp.ApprovedById,
                ApprovedByName = rp.ApprovedBy != null ? rp.ApprovedBy.FullName : null,
                ApprovedAt = rp.ApprovedAt,
                CreatedAt = rp.CreatedAt,
                UpdatedAt = rp.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (recruitmentPlan == null)
        {
            throw new Exception("Không tìm thấy kế hoạch tuyển dụng");
        }

        // 3. Check for DepartmentHead restrictions: Must match DepartmentId
        var userRoles = _currentUserService.Roles;
        if (userRoles != null && userRoles.Contains(AppRoles.DepartmentHead))
        {
            var userDepartmentId = await _currentUserService.GetDepartmentIdAsync();
            if (userDepartmentId == null)
            {
                throw new UnauthorizedAccessException("Người dùng không thuộc phòng ban nào");
            }

            var planDepartmentId = await _context.RecruitmentPlans
                .Where(rp => rp.Id == recruitmentPlan.Id)
                .Select(rp => rp.DepartmentId)
                .FirstOrDefaultAsync(cancellationToken);

            if (planDepartmentId != userDepartmentId.Value)
            {
                throw new UnauthorizedAccessException("Bạn chỉ có quyền xem kế hoạch tuyển dụng của phòng ban mình");
            }
        }

        return recruitmentPlan;
    }
}
