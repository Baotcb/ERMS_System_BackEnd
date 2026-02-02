using ERMS.Application.Interface;
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

        return recruitmentPlan;
    }
}
