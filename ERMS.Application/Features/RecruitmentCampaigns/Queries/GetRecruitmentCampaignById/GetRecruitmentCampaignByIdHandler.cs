using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.RecruitmentCampaigns.Queries.GetRecruitmentCampaignById;

public sealed class GetRecruitmentCampaignByIdHandler : IRequestHandler<GetRecruitmentCampaignByIdQuery, RecruitmentCampaignDetailDto>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetRecruitmentCampaignByIdHandler(IERMSDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<RecruitmentCampaignDetailDto> Handle(GetRecruitmentCampaignByIdQuery request, CancellationToken cancellationToken)
    {
        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();
        if (enterpriseId == null)
        {
            throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào");
        }

        var campaign = await _context.RecruitmentCampaigns
            .Where(c => c.Id == request.Id
                       && c.EnterpriseId == enterpriseId.Value
                       && !c.IsDeleted)
            .Select(c => new RecruitmentCampaignDetailDto
            {
                Id = c.Id,
                EnterpriseId = c.EnterpriseId,
                CampaignName = c.CampaignName,
                CampaignCode = c.CampaignCode,
                Description = c.Description,
                FiscalYear = c.FiscalYear,
                FiscalQuarter = c.FiscalQuarter,
                SubmissionStartDate = c.SubmissionStartDate,
                SubmissionEndDate = c.SubmissionEndDate,
                TargetHireStartDate = c.TargetHireStartDate,
                TargetHireEndDate = c.TargetHireEndDate,
                TotalBudgetCeiling = c.TotalBudgetCeiling,
                MaxTotalPositions = c.MaxTotalPositions,
                Status = c.Status,
                CreatedById = c.CreatedById,
                CreatedByName = c.CreatedBy.FullName,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                TotalPlansCount = c.RecruitmentPlans.Count(p => !p.IsDeleted)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (campaign == null)
        {
            throw new Exception("Không tìm thấy chiến dịch tuyển dụng");
        }

        return campaign;
    }
}
