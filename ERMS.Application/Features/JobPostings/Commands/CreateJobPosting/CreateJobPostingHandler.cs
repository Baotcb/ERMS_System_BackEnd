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
    private readonly ISubscriptionLimitChecker _subscriptionLimitChecker;
    private readonly ILogger<CreateJobPostingHandler> _logger;

    public CreateJobPostingHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        ISubscriptionLimitChecker subscriptionLimitChecker,
        ILogger<CreateJobPostingHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _subscriptionLimitChecker = subscriptionLimitChecker;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateJobPostingCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate current user
        var userId = _currentUserService.UserId 
            ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

        var userRoles = _currentUserService.Roles;
        if (userRoles == null || !userRoles.Contains(AppRoles.HRManager))
        {
            throw new UnauthorizedAccessException("Chỉ HR Manager mới có quyền tạo tin tuyển dụng.");
        }

        // 2. Get enterprise context
        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync()
            ?? throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào.");

        // 2.1. Subscription limit check for number of job postings
        var jobPostingLimit = await _subscriptionLimitChecker.CheckJobPostingLimitAsync(enterpriseId, cancellationToken);
        if (!jobPostingLimit.IsAllowed)
        {
            throw new InvalidOperationException(jobPostingLimit.Message ?? "Đã đạt giới hạn số tin tuyển dụng.");
        }

        // 3. Load PlanDetail with RecruitmentPlan
        var planDetail = await _context.PlanDetails
            .Include(pd => pd.RecruitmentPlan)
            .FirstOrDefaultAsync(pd => 
                pd.Id == request.PlanDetailId 
                && pd.RecruitmentPlan.EnterpriseId == enterpriseId
                && !pd.IsDeleted, 
                cancellationToken)
            ?? throw new Exception($"Không tìm thấy chi tiết kế hoạch với ID {request.PlanDetailId}.");

        // 4. VALIDATION 1: Parent RecruitmentPlan must be Approved by Director
        if (!PlanStatus.IsApproved(planDetail.RecruitmentPlan.Status))
        {
            throw new Exception($"Kế hoạch tuyển dụng phải được Giám đốc phê duyệt trước khi tạo tin tuyển dụng. Hiện tại: {planDetail.RecruitmentPlan.Status}");
        }

        // 5. VALIDATION 2: PlanDetail status must be "Approved"
        if (!PlanDetailStatus.IsApproved(planDetail.Status))
        {
            throw new Exception($"Trạng thái chi tiết kế hoạch phải là 'Approved'. Hiện tại: {planDetail.Status}");
        }

        // 6. VALIDATION 3: Quota Check
        var hiredCount = await _context.Applications
            .Where(a => a.JobPosting.PlanDetailId == request.PlanDetailId
                     && a.Stage == ApplicationStage.Hired
                     && !a.IsDeleted)
            .CountAsync(cancellationToken);

        if (hiredCount >= planDetail.Quantity)
        {
            throw new Exception($"Hết chỉ tiêu. Yêu cầu: {planDetail.Quantity}, Đã tuyển: {hiredCount}");
        }

        // 7. VALIDATION 4: RequiredSkills must not be empty (Critical for AI CV scanning)
        if (string.IsNullOrWhiteSpace(planDetail.RequiredSkills))
        {
            throw new Exception("Yêu cầu kỹ năng trong chi tiết kế hoạch trống. Chức năng sàng lọc CV bằng AI cần yêu cầu công việc để hoạt động.");
        }

        var salaryRangeMin = request.SalaryRangeMin ?? planDetail.SalaryRangeMin;
        var salaryRangeMax = request.SalaryRangeMax ?? planDetail.SalaryRangeMax;
        if (salaryRangeMin.HasValue && salaryRangeMax.HasValue && salaryRangeMax.Value < salaryRangeMin.Value)
        {
            throw new Exception("SalaryRangeMax phải lớn hơn hoặc bằng SalaryRangeMin.");
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
            SalaryRangeMin = salaryRangeMin,
            SalaryRangeMax = salaryRangeMax,
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
            ShowSalary = request.ShowSalary ?? true,
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
