using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.RecruitmentCampaigns.Queries.GetAllRecruitmentCampaigns;

public sealed class GetAllRecruitmentCampaignsHandler : IRequestHandler<GetAllRecruitmentCampaignsQuery, GetAllRecruitmentCampaignsResult>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetAllRecruitmentCampaignsHandler(IERMSDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<GetAllRecruitmentCampaignsResult> Handle(GetAllRecruitmentCampaignsQuery request, CancellationToken cancellationToken)
    {
        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();
        if (enterpriseId == null)
        {
            throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào");
        }

        var query = _context.RecruitmentCampaigns
            .Where(c => c.EnterpriseId == enterpriseId.Value && !c.IsDeleted)
            .AsQueryable();


        if (!string.IsNullOrEmpty(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(c =>
                c.CampaignName.ToLower().Contains(search) ||
                c.CampaignCode.ToLower().Contains(search));
        }

      
        if (!string.IsNullOrEmpty(request.Status))
        {
            query = query.Where(c => c.Status == request.Status);
        }

        if (request.FiscalYear.HasValue)
        {
            query = query.Where(c => c.FiscalYear == request.FiscalYear.Value);
        }

       
        if (request.FiscalQuarter.HasValue)
        {
            query = query.Where(c => c.FiscalQuarter == request.FiscalQuarter.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new RecruitmentCampaignDto
            {
                Id = c.Id,
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
                CreatedByName = c.CreatedBy.FullName,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                TotalPlansCount = c.RecruitmentPlans.Count(p => !p.IsDeleted),
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
            .ToListAsync(cancellationToken);

        return new GetAllRecruitmentCampaignsResult
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}

