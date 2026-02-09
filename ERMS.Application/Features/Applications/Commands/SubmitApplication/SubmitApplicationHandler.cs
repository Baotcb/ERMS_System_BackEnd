using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Constants.Recruitment;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Application;
using ERMS.Domain.Entities.Candidate;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

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
    private readonly IGeminiAIService _geminiAIService;
    private readonly ILogger<SubmitApplicationHandler> _logger;

    public SubmitApplicationHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        ICloudinaryService cloudinaryService,
        IPdfTextExtractor pdfTextExtractor,
        IGeminiAIService geminiAIService,
        ILogger<SubmitApplicationHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _cloudinaryService = cloudinaryService;
        _pdfTextExtractor = pdfTextExtractor;
        _geminiAIService = geminiAIService;
        _logger = logger;
    }

    public async Task<SubmitApplicationResult> Handle(SubmitApplicationCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate current user is authenticated
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        var userRoles = _currentUserService.Roles;
        if (userRoles == null || !userRoles.Contains(AppRoles.Candidate))
        {
            throw new UnauthorizedAccessException("Only candidates can submit job applications.");
        }

        // 2. Get candidate profile
        var candidate = await _context.Candidates
            .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted, cancellationToken)
            ?? throw new Exception("Candidate profile not found. Please complete your profile first.");

        // 3. Load and validate JobPosting
        var jobPosting = await _context.JobPostings
            .Include(jp => jp.PlanDetail)
            .FirstOrDefaultAsync(jp => jp.Id == request.JobPostingId && !jp.IsDeleted, cancellationToken)
            ?? throw new Exception($"Job posting with ID {request.JobPostingId} not found.");

        // 4. Validate job posting is published and accepting applications
        if (!JobPostingStatus.IsPublished(jobPosting.Status))
        {
            throw new Exception($"Job posting is not open for applications. Current status: {jobPosting.Status}");
        }

        if (jobPosting.ApplicationDeadline.HasValue && jobPosting.ApplicationDeadline.Value < DateTime.UtcNow)
        {
            throw new Exception("Application deadline has passed for this job posting.");
        }

        // 5. Check for duplicate application
        var existingApplication = await _context.Applications
            .AnyAsync(a => a.JobPostingId == request.JobPostingId
                        && a.CandidateId == candidate.Id
                        && !a.IsDeleted, cancellationToken);

        if (existingApplication)
        {
            throw new Exception("You have already applied for this job posting.");
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

        // 8. Call Gemini AI for CV scoring (with graceful degradation)
        CVScreeningResultDto aiResult;
        bool aiScoringSucceeded = true;

        try
        {
            aiResult = await _geminiAIService.AnalyzeResumeAsync(
                resumeText,
                jobPosting.Description,
                jobPosting.Requirements ?? "",
                jobPosting.EducationLevel,
                jobPosting.ExperienceLevel);
        }
        catch (Exception ex)
        {
            // Log the error but don't fail the entire application submission
            _logger.LogWarning(ex, "AI scoring failed for candidate {CandidateId}. Using default scores.", candidate.Id);
            aiScoringSucceeded = false;

            // Create default result with 0 scores - HR can manually review
            aiResult = new CVScreeningResultDto
            {
                OverallScore = 0,
                SkillMatchScore = 0,
                ExperienceMatchScore = 0,
                EducationMatchScore = 0,
                KeywordMatchScore = 0,
                MatchedSkills = [],
                MissingSkills = [],
                Strengths = [],
                Concerns = ["AI scoring failed - manual review required"],
                Summary = $"AI scoring unavailable: {ex.Message}. Please review manually.",
                RawResponse = ex.ToString()
            };
        }

        // 9. Create Resume entity
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

        // 10. Create Application entity
        var application = new Domain.Entities.Application.Application
        {
            Id = Guid.CreateVersion7(),
            JobPostingId = request.JobPostingId,
            CandidateId = candidate.Id,
            ResumeId = resume.Id,
            CoverLetter = request.CoverLetter?.Trim(),
            ExpectedSalary = request.ExpectedSalary,
            AvailableStartDate = request.AvailableStartDate,
            Stage = ApplicationStage.Applied,
            StageUpdatedAt = DateTime.UtcNow,
            Status = "Active",
            Source = "Website",
            AppliedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        _context.Applications.Add(application);

        // 11. Create CVScreeningResult entity
        var screeningResult = new CVScreeningResult
        {
            Id = Guid.CreateVersion7(),
            ApplicationId = application.Id,
            OverallScore = aiResult.OverallScore,
            SkillMatchScore = aiResult.SkillMatchScore,
            ExperienceMatchScore = aiResult.ExperienceMatchScore,
            EducationMatchScore = aiResult.EducationMatchScore,
            KeywordMatchScore = aiResult.KeywordMatchScore,
            MatchedSkills = JsonSerializer.Serialize(aiResult.MatchedSkills),
            MissingSkills = JsonSerializer.Serialize(aiResult.MissingSkills),
            Strengths = JsonSerializer.Serialize(aiResult.Strengths),
            Concerns = JsonSerializer.Serialize(aiResult.Concerns),
            Summary = aiResult.Summary,
            RawResponse = aiResult.RawResponse,
            ProcessedAt = DateTime.UtcNow,
            AIModel = "gemini-2.5-flash"
        };

        _context.CVScreeningResults.Add(screeningResult);

        // 12. Increment application count on job posting
        jobPosting.ApplicationCount++;
        jobPosting.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Application {ApplicationId} submitted successfully. CV Score: {Score}",
            application.Id, aiResult.OverallScore);

        // 13. Return result
        return new SubmitApplicationResult
        {
            ApplicationId = application.Id,
            ResumeId = resume.Id,
            ResumeUrl = resumeUrl,
            Stage = application.Stage,
            AppliedAt = application.AppliedAt,
            CVScreeningResult = new CVScreeningResultSummary
            {
                OverallScore = aiResult.OverallScore,
                SkillMatchScore = aiResult.SkillMatchScore,
                ExperienceMatchScore = aiResult.ExperienceMatchScore,
                EducationMatchScore = aiResult.EducationMatchScore,
                MatchedSkills = aiResult.MatchedSkills,
                MissingSkills = aiResult.MissingSkills,
                Strengths = aiResult.Strengths,
                Summary = aiResult.Summary
            }
        };
    }
}
