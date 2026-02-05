using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.JobPostings.Queries.GetJobPostingById;

public sealed class GetJobPostingByIdHandler : IRequestHandler<GetJobPostingByIdQuery, JobPostingDetailDto?>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetJobPostingByIdHandler(IERMSDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<JobPostingDetailDto?> Handle(GetJobPostingByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        var userRoles = _currentUserService.Roles;
        if (userRoles == null || (!userRoles.Contains(AppRoles.HRManager) && !userRoles.Contains(AppRoles.Director)))
        {
            throw new UnauthorizedAccessException("Only HR Manager or Director can view job posting details.");
        }

        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync()
            ?? throw new UnauthorizedAccessException("User is not associated with any enterprise.");

        var jobPosting = await _context.JobPostings
            .Include(jp => jp.Department)
            .Include(jp => jp.PlanDetail)
                .ThenInclude(pd => pd!.RecruitmentPlan)
                    .ThenInclude(rp => rp.Campaign)
            .Where(jp => jp.Id == request.Id
                      && jp.EnterpriseId == enterpriseId
                      && !jp.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (jobPosting == null)
            return null;

        int? quotaUsed = null;
        if (jobPosting.PlanDetailId.HasValue)
        {
            quotaUsed = await _context.Applications
                .Where(a => a.JobPosting.PlanDetailId == jobPosting.PlanDetailId
                         && a.Stage == ApplicationStage.Hired
                         && !a.IsDeleted)
                .CountAsync(cancellationToken);
        }

        return new JobPostingDetailDto
        {
            Id = jobPosting.Id,
            JobTitle = jobPosting.JobTitle,
            JobCode = jobPosting.JobCode,
            Description = jobPosting.Description,
            Requirements = jobPosting.Requirements,
            Benefits = jobPosting.Benefits,
            EmploymentType = jobPosting.EmploymentType,
            ExperienceLevel = jobPosting.ExperienceLevel,
            EducationLevel = jobPosting.EducationLevel,
            SalaryRangeMin = jobPosting.SalaryRangeMin,
            SalaryRangeMax = jobPosting.SalaryRangeMax,
            ShowSalary = jobPosting.ShowSalary,
            Location = jobPosting.Location,
            RemoteOption = jobPosting.RemoteOption,
            Quantity = jobPosting.Quantity,
            ApplicationDeadline = jobPosting.ApplicationDeadline,
            Status = jobPosting.Status,
            PublishedAt = jobPosting.PublishedAt,
            ClosedAt = jobPosting.ClosedAt,
            ViewCount = jobPosting.ViewCount,
            ApplicationCount = jobPosting.ApplicationCount,
            CreatedAt = jobPosting.CreatedAt,
            DepartmentName = jobPosting.Department.DepartmentName,
            PlanDetailId = jobPosting.PlanDetailId,
            PlanName = jobPosting.PlanDetail?.RecruitmentPlan?.PlanName,
            CampaignName = jobPosting.PlanDetail?.RecruitmentPlan?.Campaign?.CampaignName,
            QuotaUsed = quotaUsed,
            QuotaTotal = jobPosting.PlanDetail?.Quantity
        };
    }
}
