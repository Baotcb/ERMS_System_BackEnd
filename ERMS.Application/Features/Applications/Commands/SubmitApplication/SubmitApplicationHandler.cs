using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Constants.Recruitment;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Candidate;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.Applications.Commands.SubmitApplication;

/// <summary>
/// Handler for submitting job applications with CV processing and AI scoring
/// </summary>
public sealed class SubmitApplicationHandler : IRequestHandler<SubmitApplicationCommand, SubmitApplicationResult>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly IPdfTextExtractor _pdfTextExtractor;
    private readonly IBackgroundTaskQueue _backgroundQueue;
    private readonly ISubscriptionLimitChecker _subscriptionLimitChecker;
    private readonly ILogger<SubmitApplicationHandler> _logger;

    public SubmitApplicationHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        ICloudinaryService cloudinaryService,
        IPdfTextExtractor pdfTextExtractor,
        IBackgroundTaskQueue backgroundQueue,
        ISubscriptionLimitChecker subscriptionLimitChecker,
        ILogger<SubmitApplicationHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _cloudinaryService = cloudinaryService;
        _pdfTextExtractor = pdfTextExtractor;
        _backgroundQueue = backgroundQueue;
        _subscriptionLimitChecker = subscriptionLimitChecker;
        _logger = logger;
    }

    public async Task<SubmitApplicationResult> Handle(SubmitApplicationCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate current user is authenticated
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

        var userRoles = _currentUserService.Roles;
        if (userRoles == null || !userRoles.Contains(AppRoles.Candidate))
        {
            throw new UnauthorizedAccessException("Chỉ ứng viên mới có quyền nộp hồ sơ ứng tuyển.");
        }

        // 2. Get candidate profile
        var candidate = await _context.Candidates
            .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted, cancellationToken)
            ?? throw new Exception("Không tìm thấy hồ sơ ứng viên. Vui lòng hoàn thành hồ sơ của bạn trước.");

        // 3. Load and validate JobPosting
        var jobPosting = await _context.JobPostings
            .FirstOrDefaultAsync(jp => jp.Id == request.JobPostingId && !jp.IsDeleted, cancellationToken)
            ?? throw new Exception($"Không tìm thấy tin tuyển dụng với ID {request.JobPostingId}.");

        // 4. Validate job posting is published and accepting applications
        if (!JobPostingStatus.IsPublished(jobPosting.Status))
        {
            throw new Exception($"Tin tuyển dụng không mở nhận hồ sơ. Trạng thái hiện tại: {jobPosting.Status}");
        }

        if (jobPosting.ApplicationDeadline.HasValue && jobPosting.ApplicationDeadline.Value < DateTime.UtcNow)
        {
            throw new Exception("Hạn nộp hồ sơ cho tin tuyển dụng này đã qua.");
        }

        // 5. Check for duplicate application
        var existingApplication = await _context.Applications
            .AnyAsync(a => a.JobPostingId == request.JobPostingId
                        && a.CandidateId == candidate.Id
                        && !a.IsDeleted, cancellationToken);

        if (existingApplication)
        {
            throw new Exception("Bạn đã ứng tuyển cho tin tuyển dụng này rồi.");
        }

        // 6. Upload CV to Cloudinary
        _logger.LogInformation("Uploading CV for candidate {CandidateId} to job {JobPostingId}",
            candidate.Id, request.JobPostingId);

        string resumeUrl, resumePublicId;
        await using (var stream = request.CvFile.OpenReadStream())
        {
            (resumeUrl, resumePublicId) = await _cloudinaryService.UploadPdfAsync(stream, request.CvFile.FileName);
        }

        // 7. Extract text from PDF for AI analysis
        string resumeText;
        await using (var stream = request.CvFile.OpenReadStream())
        {
            // Copy to MemoryStream for extraction
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;
            resumeText = await _pdfTextExtractor.ExtractTextAsync(memoryStream);
        }

        // 8. Create Resume entity
        var resume = new Resume
        {
            Id = Guid.CreateVersion7(),
            CandidateId = candidate.Id,
            FileName = request.CvFile.FileName,
            FileUrl = resumeUrl,
            FileSize = (int)request.CvFile.Length,
            FileType = "application/pdf",
            IsDefault = false,
            ParsedData = resumeText.Length > 10000 ? resumeText[..10000] : resumeText, // Store first 10K chars
            UploadedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        _context.Resumes.Add(resume);

        // 9. Create Application entity
        var application = new Domain.Entities.Application.Application
        {
            Id = Guid.CreateVersion7(),
            JobPostingId = request.JobPostingId,
            CandidateId = candidate.Id,
            ResumeId = resume.Id,
            CoverLetter = request.CoverLetter?.Trim(),
            ExpectedSalary = request.ExpectedSalary ?? 0,
            AvailableStartDate = request.AvailableStartDate,
            Stage = ApplicationStage.Applied,
            StageUpdatedAt = DateTime.UtcNow,
            Status = "Active",
            Source = "Website",
            AppliedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        _context.Applications.Add(application);

        // 10. Increment application count on job posting
        jobPosting.ApplicationCount++;
        jobPosting.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // 11. Enqueue CV scoring only for PRO plan enterprises
        var isProPlan = await _subscriptionLimitChecker.IsProPlanAsync(jobPosting.EnterpriseId, cancellationToken);
        if (isProPlan)
        {
            await _backgroundQueue.EnqueueAsync(new CvScoringWorkItem(
                ApplicationId: application.Id,
                ResumeText: resumeText,
                JobDescription: jobPosting.Description,
                RequiredSkills: jobPosting.Requirements ?? "",
                EducationLevel: jobPosting.EducationLevel,
                ExperienceLevel: jobPosting.ExperienceLevel
            ), cancellationToken);

            _logger.LogInformation(
                "Application {ApplicationId} submitted successfully. CV scoring enqueued for Pro enterprise {EnterpriseId}.",
                application.Id,
                jobPosting.EnterpriseId);
        }
        else
        {
            _logger.LogInformation(
                "Application {ApplicationId} submitted successfully. CV scoring skipped for Free enterprise {EnterpriseId}.",
                application.Id,
                jobPosting.EnterpriseId);
        }

        // 12. Return result immediately (CV scoring runs in background)
        return new SubmitApplicationResult
        {
            ApplicationId = application.Id,
            ResumeId = resume.Id,
            ResumeUrl = resumeUrl,
            Stage = application.Stage,
            AppliedAt = application.AppliedAt,
            CVScreeningResult = null
        };
    }
}
