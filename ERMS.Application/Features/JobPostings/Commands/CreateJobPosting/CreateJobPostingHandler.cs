using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Constants.Recruitment;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Recruitment;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.JobPostings.Commands.CreateJobPosting;

public sealed class CreateJobPostingHandler : IRequestHandler<CreateJobPostingCommand, Guid>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreateJobPostingHandler> _logger;

    public CreateJobPostingHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        ILogger<CreateJobPostingHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateJobPostingCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate current user
        var userId = _currentUserService.UserId 
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        var userRoles = _currentUserService.Roles;
        if (userRoles == null || !userRoles.Contains(AppRoles.HRManager))
        {
            throw new UnauthorizedAccessException("Only HR Manager can create job postings.");
        }

        // 2. Get enterprise context
        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync()
            ?? throw new UnauthorizedAccessException("User is not associated with any enterprise.");

        // 3. Load PlanDetail with RecruitmentPlan
        var planDetail = await _context.PlanDetails
            .Include(pd => pd.RecruitmentPlan)
            .FirstOrDefaultAsync(pd => 
                pd.Id == request.PlanDetailId 
                && pd.RecruitmentPlan.EnterpriseId == enterpriseId
                && !pd.IsDeleted, 
                cancellationToken)
            ?? throw new Exception($"PlanDetail with ID {request.PlanDetailId} not found.");

        // 4. VALIDATION 1: Parent RecruitmentPlan must be Approved by Director
        if (!PlanStatus.IsApproved(planDetail.RecruitmentPlan.Status))
        {
            throw new Exception($"BusinessRuleException: RecruitmentPlan must be 'Approved' by Director before creating JobPosting. Current: {planDetail.RecruitmentPlan.Status}");
        }

        // 5. VALIDATION 2: PlanDetail status must be "Approved"
        if (!PlanDetailStatus.IsApproved(planDetail.Status))
        {
            throw new Exception($"BusinessRuleException: PlanDetail status must be 'Approved'. Current: {planDetail.Status}");
        }

        // 6. VALIDATION 3: Quota Check
        var hiredCount = await _context.Applications
            .Where(a => a.JobPosting.PlanDetailId == request.PlanDetailId
                     && a.Stage == ApplicationStage.Hired
                     && !a.IsDeleted)
            .CountAsync(cancellationToken);

        if (hiredCount >= planDetail.Quantity)
        {
            throw new Exception($"BusinessRuleException: Quota exhausted. Required: {planDetail.Quantity}, Hired: {hiredCount}");
        }

        // 7. VALIDATION 4: RequiredSkills must not be empty (Critical for AI CV scanning)
        if (string.IsNullOrWhiteSpace(planDetail.RequiredSkills))
        {
            throw new Exception("BusinessRuleException: PlanDetail.RequiredSkills is empty. AI CV scanning requires job requirements to function properly.");
        }

        // 8. Create JobPosting with AUTO-FILL from PlanDetail
        var jobPosting = new JobPosting
        {
            Id = Guid.CreateVersion7(),
            EnterpriseId = enterpriseId,
            PlanDetailId = request.PlanDetailId,
            
            // AUTO-FILL from PlanDetail
            DepartmentId = planDetail.RecruitmentPlan.DepartmentId,
            JobTitle = !string.IsNullOrWhiteSpace(request.TitleOverride) 
                ? request.TitleOverride.Trim() 
                : planDetail.PositionTitle,
            
            // CRITICAL: Copy RequiredSkills to Requirements for AI CV scanning
            Requirements = planDetail.RequiredSkills,
            
            ExperienceLevel = planDetail.MinExperience.HasValue 
                ? $"{planDetail.MinExperience}-{planDetail.MaxExperience} years" 
                : null,
            EducationLevel = planDetail.EducationLevel,
            SalaryRangeMin = planDetail.SalaryRangeMin,
            SalaryRangeMax = planDetail.SalaryRangeMax,
            Quantity = planDetail.Quantity,
            EmploymentType = "Full-time",
            
            // User input
            Description = !string.IsNullOrWhiteSpace(request.DescriptionOverride)
                ? request.DescriptionOverride.Trim()
                : $"We are looking for a {planDetail.PositionTitle}.",
            Benefits = request.Benefits?.Trim(),
            Location = request.Location?.Trim(),
            RemoteOption = request.RemoteOption?.Trim(),
            ApplicationDeadline = request.ApplicationDeadline,
            
            // Initial state
            Status = JobPostingStatus.Draft,
            ShowSalary = true,
            ViewCount = 0,
            ApplicationCount = 0,
            CreatedById = userId,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.JobPostings.Add(jobPosting);

        // Update PlanDetail status to Recruiting
        var existingPostings = await _context.JobPostings
            .CountAsync(jp => jp.PlanDetailId == request.PlanDetailId && !jp.IsDeleted, cancellationToken);

        if (existingPostings == 0)
        {
            planDetail.Status = PlanDetailStatus.Recruiting;
            planDetail.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created JobPosting {JobPostingId} from PlanDetail {PlanDetailId}. Quota: {Hired}/{Total}",
            jobPosting.Id, request.PlanDetailId, hiredCount, planDetail.Quantity);

        return jobPosting.Id;
    }
}
