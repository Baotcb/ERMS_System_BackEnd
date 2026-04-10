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
            ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

        var userRoles = _currentUserService.Roles;
        if (userRoles == null || (!userRoles.Contains(AppRoles.HRManager) && !userRoles.Contains(AppRoles.Director)))
            throw new UnauthorizedAccessException("Chỉ HR Manager hoặc Giám đốc mới có quyền xem chi tiết tin tuyển dụng.");

        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync()
            ?? throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào.");

        var jobPosting = await _context.JobPostings
            .Include(jp => jp.Department)
            .Include(jp => jp.CreatedBy)
            .Include(jp => jp.PublishedBy)
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

        // Application pipeline counts per stage
        var stageCounts = await _context.Applications
            .Where(a => a.JobPostingId == jobPosting.Id && !a.IsDeleted)
            .GroupBy(a => a.Stage)
            .Select(g => new { Stage = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        int GetCount(string stage) => stageCounts.FirstOrDefault(s => string.Equals(s.Stage, stage, StringComparison.OrdinalIgnoreCase))?.Count ?? 0;

        // Audit history
        var history = await _context.ApprovalHistories
            .Where(h => h.EntityType == "JobPosting" && h.EntityId == jobPosting.Id)
            .OrderByDescending(h => h.CreatedAt)
            .Select(h => new JobPostingHistoryDto
            {
                Action = h.Action,
                PreviousStatus = h.PreviousStatus,
                NewStatus = h.NewStatus,
                PerformedByName = h.PerformedBy.FullName,
                Note = h.Note,
                CreatedAt = h.CreatedAt
            })
            .ToListAsync(cancellationToken);

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
            UpdatedAt = jobPosting.UpdatedAt,
            CreatedByName = jobPosting.CreatedBy?.FullName,
            PublishedByName = jobPosting.PublishedBy?.FullName,
            DepartmentName = jobPosting.Department.DepartmentName,
            PlanDetailId = jobPosting.PlanDetailId,
            PlanName = jobPosting.PlanDetail?.RecruitmentPlan?.PlanName,
            CampaignName = jobPosting.PlanDetail?.RecruitmentPlan?.Campaign?.CampaignName,
            QuotaUsed = quotaUsed,
            QuotaTotal = jobPosting.PlanDetail?.Quantity,
            TotalApplications = stageCounts.Sum(s => s.Count),
            AppliedCount = GetCount(ApplicationStage.Applied),
            ReviewingCount = GetCount(ApplicationStage.Reviewing),
            ShortlistedCount = GetCount(ApplicationStage.Shortlisted),
            InterviewScheduledCount = GetCount(ApplicationStage.InterviewScheduled),
            InterviewedCount = GetCount(ApplicationStage.Interviewed),
            OfferProcessingCount = GetCount(ApplicationStage.OfferProcessing),
            OfferedCount = GetCount(ApplicationStage.Offered),
            HiredCount = GetCount(ApplicationStage.Hired),
            RejectedCount = GetCount(ApplicationStage.Rejected),
            WithdrawnCount = GetCount(ApplicationStage.Withdrawn),
            History = history
        };
    }
}
