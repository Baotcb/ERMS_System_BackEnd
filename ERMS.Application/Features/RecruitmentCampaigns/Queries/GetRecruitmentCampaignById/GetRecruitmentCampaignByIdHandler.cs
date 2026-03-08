using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
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
                TotalPlansCount = c.RecruitmentPlans.Count(p => !p.IsDeleted),
                UsedBudget = c.RecruitmentPlans
                    .Where(p => !p.IsDeleted && p.Status == "Approved")
                    .Sum(p => p.TotalBudget ?? 0),
                PendingBudget = c.RecruitmentPlans
                    .Where(p => !p.IsDeleted && p.Status == "Pending")
                    .Sum(p => p.TotalBudget ?? 0),
                RemainingBudget = (c.TotalBudgetCeiling ?? 0) - c.RecruitmentPlans
                    .Where(p => !p.IsDeleted && p.Status == "Approved")
                    .Sum(p => p.TotalBudget ?? 0),
                ActualCost = c.RecruitmentPlans
                    .Where(p => !p.IsDeleted)
                    .SelectMany(p => p.PlanDetails.Where(pd => !pd.IsDeleted))
                    .SelectMany(pd => pd.JobPostings.Where(jp => !jp.IsDeleted))
                    .SelectMany(jp => jp.Applications.Where(a => !a.IsDeleted))
                    .Where(a => a.Offer != null && !a.Offer.IsDeleted && a.Offer.Status == OfferStatus.Accepted)
                    .Sum(a => a.Offer!.SalaryFrequency == OfferSalaryFrequency.Yearly
                        ? a.Offer.Salary / 12m
                        : a.Offer.Salary)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (campaign == null)
        {
            throw new Exception("Không tìm thấy chiến dịch tuyển dụng");
        }

        return campaign;
    }
}

