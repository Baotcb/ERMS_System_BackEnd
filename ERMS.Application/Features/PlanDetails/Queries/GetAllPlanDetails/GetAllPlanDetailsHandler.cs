using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.PlanDetails.Queries.GetAllPlanDetails;

public sealed class GetAllPlanDetailsHandler : IRequestHandler<GetAllPlanDetailsQuery, List<PlanDetailDto>>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetAllPlanDetailsHandler(IERMSDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<List<PlanDetailDto>> Handle(GetAllPlanDetailsQuery request, CancellationToken cancellationToken)
    {
        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();
        if (enterpriseId == null)
        {
            throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào");
        }

        var planDetails = await _context.PlanDetails
            .Include(d => d.RecruitmentPlan)
            .Include(d => d.RequestedBy)
            .Where(d => d.RecruitmentPlanId == request.RecruitmentPlanId
                     && d.RecruitmentPlan.EnterpriseId == enterpriseId.Value
                     && !d.IsDeleted)
            .OrderBy(d => d.CreatedAt)
            .Select(d => new PlanDetailDto
            {
                Id = d.Id,
                RecruitmentPlanId = d.RecruitmentPlanId,
                PositionTitle = d.PositionTitle,
                Quantity = d.Quantity,
                Priority = d.Priority,
                Justification = d.Justification,
                RequiredSkills = d.RequiredSkills,
                MinExperience = d.MinExperience,
                MaxExperience = d.MaxExperience,
                EducationLevel = d.EducationLevel,
                SalaryRangeMin = d.SalaryRangeMin,
                SalaryRangeMax = d.SalaryRangeMax,
                ExpectedStartDate = d.ExpectedStartDate,
                Status = d.Status,
                RequestedByName = d.RequestedBy.FullName,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return planDetails;
    }
}
