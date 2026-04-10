using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.Applications.Commands.AddExternalApplication;

public sealed class AddExternalApplicationHandler : IRequestHandler<AddExternalApplicationCommand, AddExternalApplicationResult>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IBackgroundTaskQueue _backgroundQueue;
    private readonly ISubscriptionLimitChecker _subscriptionLimitChecker;
    private readonly ILogger<AddExternalApplicationHandler> _logger;

    public AddExternalApplicationHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        IBackgroundTaskQueue backgroundQueue,
        ISubscriptionLimitChecker subscriptionLimitChecker,
        ILogger<AddExternalApplicationHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _backgroundQueue = backgroundQueue;
        _subscriptionLimitChecker = subscriptionLimitChecker;
        _logger = logger;
    }

    public async Task<AddExternalApplicationResult> Handle(AddExternalApplicationCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate HRManager role
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

        if (!_currentUserService.Roles.Contains(AppRoles.HRManager))
            throw new UnauthorizedAccessException("Chỉ HR Manager mới có quyền thêm CV.");

        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync()
            ?? throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào.");

        // 2. Load and validate job posting
        var jobPosting = await _context.JobPostings
            .FirstOrDefaultAsync(jp => jp.Id == request.JobPostingId && !jp.IsDeleted, cancellationToken)
            ?? throw new Exception("Không tìm thấy tin tuyển dụng.");

        if (jobPosting.EnterpriseId != enterpriseId)
            throw new UnauthorizedAccessException("Bạn không có quyền thêm CV cho tin tuyển dụng này.");

        if (jobPosting.Status != "Published")
            throw new Exception("Chỉ có thể thêm CV cho tin tuyển dụng đang kích hoạt.");

        // 3. Duplicate check: same email + same job posting
        var isDuplicate = await _context.Applications
            .AnyAsync(a =>
                a.JobPostingId == request.JobPostingId &&
                a.IsExternal &&
                a.ExternalCandidateEmail == request.CandidateEmail.Trim().ToLower() &&
                !a.IsDeleted,
                cancellationToken);

        if (isDuplicate)
            throw new Exception($"Ứng viên với email {request.CandidateEmail} đã có trong danh sách ứng tuyển.");

        // 4. Create Application entity
        var application = new Domain.Entities.Application.Application
        {
            Id = Guid.CreateVersion7(),
            JobPostingId = request.JobPostingId,
            CandidateId = null,
            IsExternal = true,
            ExternalCandidateName = request.CandidateName.Trim(),
            ExternalCandidateEmail = request.CandidateEmail.Trim().ToLower(),
            ExternalCandidatePhone = request.CandidatePhone?.Trim(),
            ExternalResumeUrl = request.ResumeUrl,
            Stage = ApplicationStage.Applied,
            StageUpdatedAt = DateTime.UtcNow,
            Status = "Active",
            Source = "HRAdded",
            AppliedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        _context.Applications.Add(application);

        // 5. Increment application count
        jobPosting.ApplicationCount++;
        jobPosting.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // 6. Enqueue AI CV scoring if enterprise is PRO
        var isProPlan = await _subscriptionLimitChecker.IsProPlanAsync(jobPosting.EnterpriseId, cancellationToken);
        if (isProPlan)
        {
            await _backgroundQueue.EnqueueAsync(new CvScoringWorkItem(
                ApplicationId: application.Id,
                ResumeText: request.ResumeText,
                JobDescription: jobPosting.Description,
                RequiredSkills: jobPosting.Requirements ?? "",
                EducationLevel: jobPosting.EducationLevel,
                ExperienceLevel: jobPosting.ExperienceLevel
            ), cancellationToken);
        }

        _logger.LogInformation(
            "External application created by HR {UserId} for job {JobPostingId}, candidate email: {Email}",
            userId, request.JobPostingId, request.CandidateEmail);

        return new AddExternalApplicationResult
        {
            ApplicationId = application.Id,
            Stage = application.Stage,
            AppliedAt = application.AppliedAt
        };
    }
}
