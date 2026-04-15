using ERMS.Application.Exceptions;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Constants.Recruitment;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Candidate;
using ERMS.Domain.Entities.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.Applications.Commands.AddExternalApplication;

public sealed class AddExternalApplicationHandler : IRequestHandler<AddExternalApplicationCommand, AddExternalApplicationResult>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISubscriptionLimitChecker _subscriptionLimitChecker;
    private readonly IBackgroundTaskQueue _backgroundTaskQueue;
    private readonly ILogger<AddExternalApplicationHandler> _logger;

    public AddExternalApplicationHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        ISubscriptionLimitChecker subscriptionLimitChecker,
        IBackgroundTaskQueue backgroundTaskQueue,
        ILogger<AddExternalApplicationHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _subscriptionLimitChecker = subscriptionLimitChecker;
        _backgroundTaskQueue = backgroundTaskQueue;
        _logger = logger;
    }

    public async Task<AddExternalApplicationResult> Handle(AddExternalApplicationCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

        var roles = _currentUserService.Roles;
        if (roles == null || !roles.Contains(AppRoles.HRManager))
        {
            throw new UnauthorizedAccessException("Chỉ HR Manager mới có quyền thêm hồ sơ ứng viên ngoài.");
        }

        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync()
            ?? throw new UnauthorizedAccessException("Không tìm thấy thông tin doanh nghiệp.");

        var jobPosting = await _context.JobPostings
            .FirstOrDefaultAsync(j =>
                j.Id == request.JobPostingId &&
                j.EnterpriseId == enterpriseId &&
                !j.IsDeleted,
                cancellationToken)
            ?? throw new BusinessException($"Không tìm thấy tin tuyển dụng với ID {request.JobPostingId}.");

        if (!JobPostingStatus.IsPublished(jobPosting.Status))
        {
            throw new BusinessException($"Tin tuyển dụng không mở nhận hồ sơ. Trạng thái hiện tại: {jobPosting.Status}");
        }

        if (jobPosting.ApplicationDeadline.HasValue && jobPosting.ApplicationDeadline.Value < DateTime.UtcNow)
        {
            throw new BusinessException("Hạn nộp hồ sơ cho tin tuyển dụng này đã qua.");
        }

        var normalizedEmail = request.CandidateEmail.Trim().ToLowerInvariant();

        // Check 1: email đã có trong bảng Users (ứng viên có tài khoản)
        var existingUser = await _context.Users
            .AnyAsync(u =>
                u.Email != null &&
                u.Email.ToLower() == normalizedEmail,
                cancellationToken);
        if (existingUser)
        {
            throw new BusinessException("Email ứng viên đã tồn tại trong hệ thống.");
        }

        // Check 2: email đã có trong bảng ExternalCandidates của enterprise này (tránh record mồ côi)
        var existingExternal = await _context.ExternalCandidates
            .AnyAsync(ec =>
                !ec.IsDeleted &&
                ec.EnterpriseId == enterpriseId &&
                ec.Email != null &&
                ec.Email.ToLower() == normalizedEmail,
                cancellationToken);
        if (existingExternal)
        {
            throw new BusinessException("Email ứng viên đã tồn tại trong hệ thống.");
        }

        // Check 3: đã nộp cho cùng job posting này chưa
        var duplicateApplication = await _context.Applications
            .AnyAsync(a =>
                a.JobPostingId == request.JobPostingId &&
                !a.IsDeleted &&
                (
                    (a.ExternalCandidateId != null &&
                     a.ExternalCandidate != null &&
                     a.ExternalCandidate.Email != null &&
                     a.ExternalCandidate.Email.ToLower() == normalizedEmail)
                    ||
                    (a.Candidate != null &&
                     a.Candidate.User != null &&
                     a.Candidate.User.Email != null &&
                     a.Candidate.User.Email.ToLower() == normalizedEmail)
                ),
                cancellationToken);

        if (duplicateApplication)
        {
            throw new BusinessException("Ứng viên với email này đã tồn tại trong tin tuyển dụng.");
        }

        var externalCandidate = new ExternalCandidate
        {
            Id = Guid.CreateVersion7(),
            FullName = request.CandidateName.Trim(),
            Email = request.CandidateEmail.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(request.CandidatePhone) ? null : request.CandidatePhone.Trim(),
            Source = "HRImported",
            EnterpriseId = enterpriseId,
            CreatedById = userId,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        var placeholderEmail = $"external-{Guid.NewGuid():N}@placeholder.local";
        var placeholderUser = new User
        {
            Id = Guid.CreateVersion7(),
            UserName = placeholderEmail,
            Email = placeholderEmail,
            FullName = externalCandidate.FullName,
            PhoneNumber = externalCandidate.PhoneNumber,
            EmailConfirmed = false,
            DateJoined = DateTime.UtcNow
        };

        var candidate = new Candidate
        {
            Id = Guid.CreateVersion7(),
            UserId = placeholderUser.Id,
            User = placeholderUser,
            CurrentPosition = externalCandidate.CurrentPosition,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        var parsedData = request.ResumeText.Length > 10000
            ? request.ResumeText[..10000]
            : request.ResumeText;

        var resume = new Resume
        {
            Id = Guid.CreateVersion7(),
            CandidateId = candidate.Id,
            FileName = "external-cv.pdf",
            FileUrl = request.ResumeUrl,
            FileType = "application/pdf",
            ParsedData = parsedData,
            UploadedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        var appliedAt = DateTime.UtcNow;
        var application = new Domain.Entities.Application.Application
        {
            Id = Guid.CreateVersion7(),
            JobPostingId = jobPosting.Id,
            CandidateId = candidate.Id,
            ExternalCandidateId = externalCandidate.Id,
            ResumeId = resume.Id,
            Stage = ApplicationStage.Applied,
            StageUpdatedAt = appliedAt,
            Status = "Active",
            Source = "HRImported",
            AppliedAt = appliedAt,
            IsDeleted = false
        };

        _context.ExternalCandidates.Add(externalCandidate);
        _context.Users.Add(placeholderUser);
        _context.Candidates.Add(candidate);
        _context.Resumes.Add(resume);
        _context.Applications.Add(application);

        jobPosting.ApplicationCount++;
        jobPosting.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var isProPlan = await _subscriptionLimitChecker.IsProPlanAsync(jobPosting.EnterpriseId, cancellationToken);
        if (isProPlan)
        {
            await _backgroundTaskQueue.EnqueueAsync(new CvScoringWorkItem(
                ApplicationId: application.Id,
                ResumeText: request.ResumeText,
                JobDescription: jobPosting.Description,
                RequiredSkills: jobPosting.Requirements ?? string.Empty,
                EducationLevel: jobPosting.EducationLevel,
                ExperienceLevel: jobPosting.ExperienceLevel
            ), cancellationToken);
        }

        _logger.LogInformation(
            "Hồ sơ ngoài {ApplicationId} được tạo cho công việc {JobPostingId} bởi HR {UserId}.",
            application.Id,
            jobPosting.Id,
            userId);

        return new AddExternalApplicationResult
        {
            ApplicationId = application.Id,
            Stage = application.Stage,
            AppliedAt = application.AppliedAt
        };
    }
}
