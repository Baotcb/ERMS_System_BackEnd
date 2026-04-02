using System.Text.Json;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Application;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ApplicationEntity = ERMS.Domain.Entities.Application.Application;

namespace ERMS.Application.Features.Applications.Commands.SubmitFinalDecision;

/// <summary>
/// Handler for Stage 2: Department Head submits the final decision on an interview.
/// Updates Interview status to Completed and triggers the workflow branch:
/// - Fail -> Application.Stage = Rejected
/// - Passed -> Application.Stage = OfferProcessing
/// - NextRound -> Creates new Interview with Round + 1
/// </summary>
public sealed class SubmitFinalDecisionHandler : IRequestHandler<SubmitFinalDecisionCommand, SubmitFinalDecisionResult>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<SubmitFinalDecisionHandler> _logger;
    private readonly IRejectionEmailService _rejectionEmailService;

    public SubmitFinalDecisionHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        ILogger<SubmitFinalDecisionHandler> logger,
        IRejectionEmailService rejectionEmailService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
        _rejectionEmailService = rejectionEmailService;
    }

    public async Task<SubmitFinalDecisionResult> Handle(SubmitFinalDecisionCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate current user is authenticated
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

        // 2. Get enterprise ID
        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync()
            ?? throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào.");

        // 3. Role check: DepartmentHead ONLY
        var userRoles = _currentUserService.Roles;
        if (userRoles == null || !userRoles.Contains(AppRoles.DepartmentHead))
        {
            throw new UnauthorizedAccessException("Chỉ Trưởng phòng mới có quyền đưa ra quyết định phỏng vấn cuối cùng.");
        }

        // 4. Get user's department
        var userDepartmentId = await _currentUserService.GetDepartmentIdAsync()
            ?? throw new UnauthorizedAccessException("Người dùng không thuộc phòng ban nào.");

        // 5. Load the interview with related application data
        var interview = await _context.Interviews
            .Include(i => i.Application)
                .ThenInclude(a => a.JobPosting)
            .Include(i => i.Application)
                .ThenInclude(a => a.Candidate)
                    .ThenInclude(c => c.User)
            .Include(i => i.Application)
                .ThenInclude(a => a.CVScreeningResult)
            .Include(i => i.Participants)
            .FirstOrDefaultAsync(i =>
                i.Id == request.InterviewId &&
                i.ApplicationId == request.ApplicationId &&
                !i.IsDeleted,
                cancellationToken)
            ?? throw new Exception($"Không tìm thấy buổi phỏng vấn với ID {request.InterviewId} cho hồ sơ {request.ApplicationId}.");

        // 6. Validate enterprise ownership
        if (interview.Application.JobPosting.EnterpriseId != enterpriseId)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền truy cập hồ sơ ứng tuyển này.");
        }

        // 7. Department security check
        if (interview.Application.JobPosting.DepartmentId != userDepartmentId)
        {
            throw new UnauthorizedAccessException("Bạn chỉ có thể đưa ra quyết định cho các buổi phỏng vấn trong phòng ban mình.");
        }

        // 8. Validate interview is in Scheduled status
        if (!interview.Status.Equals(InterviewStatus.Scheduled, StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception($"Không thể gửi quyết định. Trạng thái phỏng vấn là '{interview.Status}', yêu cầu '{InterviewStatus.Scheduled}'.");
        }

        var decision = InterviewDecision.ValidDecisions
            .First(validDecision => validDecision.Equals(request.Decision, StringComparison.OrdinalIgnoreCase));

        var overallFeedback = request.OverallFeedback?.Trim();
        var note = request.Note?.Trim();

        await using var transaction = await _context.BeginTransactionAsync(cancellationToken);

        try
        {
            // 9. Update interview fields
            interview.Decision = decision;
            interview.OverallRating = request.OverallRating;
            interview.OverallFeedback = overallFeedback;
            interview.Note = note;
            interview.CompletedAt = DateTime.UtcNow;
            interview.Status = InterviewStatus.Completed;
            interview.UpdatedAt = DateTime.UtcNow;

            // 10. Workflow branching
            Guid? newInterviewId = null;
            var application = interview.Application;

            switch (decision)
            {
                case InterviewDecision.Fail:
                    application.Stage = ApplicationStage.Rejected;
                    application.RejectedAt = DateTime.UtcNow;
                    application.RejectedById = userId;
                    application.RejectionReason = overallFeedback;
                    break;

                case InterviewDecision.Passed:
                    application.Stage = ApplicationStage.OfferProcessing;
                    break;

                case InterviewDecision.NextRound:
                    var nextInterview = new Interview
                    {
                        Id = Guid.CreateVersion7(),
                        ApplicationId = application.Id,
                        InterviewType = interview.InterviewType,
                        RoundNumber = interview.RoundNumber + 1,
                        Status = InterviewStatus.PendingSchedule,
                        ScheduledById = userId,
                        Note = $"Next round following Interview Round {interview.RoundNumber}.",
                        CreatedAt = DateTime.UtcNow
                    };

                    var customInterviewersProvided = request.NextRoundInterviewerIds != null && request.NextRoundInterviewerIds.Count > 0;
                    var interviewerIdsToUse = customInterviewersProvided
                        ? request.NextRoundInterviewerIds!
                        : interview.Participants.Select(p => p.EmployeeId).ToList();

                    if (customInterviewersProvided)
                    {
                        var validEmployeeIds = await _context.Employees
                            .Where(e => request.NextRoundInterviewerIds!.Contains(e.Id) && e.EnterpriseId == enterpriseId && !e.IsDeleted)
                            .Select(e => e.Id)
                            .ToListAsync(cancellationToken);

                        if (validEmployeeIds.Count != request.NextRoundInterviewerIds!.Count)
                        {
                            var missingIds = request.NextRoundInterviewerIds
                                .Where(id => !validEmployeeIds.Contains(id))
                                .ToList();

                            throw new Exception($"Một số người phỏng vấn mới không được tìm thấy hoặc không thuộc doanh nghiệp của bạn: {string.Join(", ", missingIds)}");
                        }
                    }

                    foreach (var employeeId in interviewerIdsToUse)
                    {
                        var existingParticipant = interview.Participants.FirstOrDefault(p => p.EmployeeId == employeeId);

                        nextInterview.Participants.Add(new InterviewParticipant
                        {
                            Id = Guid.CreateVersion7(),
                            EmployeeId = employeeId,
                            Role = existingParticipant?.Role ?? "Interviewer",
                            IsRequired = existingParticipant?.IsRequired ?? true,
                            ConfirmationStatus = "Pending",
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    _context.Interviews.Add(nextInterview);
                    newInterviewId = nextInterview.Id;
                    break;
            }

            application.StageUpdatedAt = DateTime.UtcNow;
            application.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            if (decision == InterviewDecision.Fail)
            {
                await TrySendRejectionEmailAsync(application, overallFeedback ?? string.Empty, cancellationToken);
            }

            _logger.LogInformation(
                "Final decision '{Decision}' submitted for Interview {InterviewId}, Application {ApplicationId} by DeptHead {UserId}. New Stage: {Stage}",
                decision,
                interview.Id,
                application.Id,
                userId,
                application.Stage);

            return new SubmitFinalDecisionResult
            {
                InterviewId = interview.Id,
                Decision = decision,
                ApplicationStage = application.Stage,
                NewInterviewId = newInterviewId
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to submit final decision for Interview {InterviewId}", request.InterviewId);
            throw;
        }
    }

    private async Task TrySendRejectionEmailAsync(ApplicationEntity application, string rejectionReason, CancellationToken cancellationToken)
    {
        var candidateUser = application.Candidate?.User;
        if (candidateUser == null || string.IsNullOrWhiteSpace(candidateUser.Email))
        {
            _logger.LogWarning(
                "Skipped rejection email for Application {ApplicationId} because candidate email is missing.",
                application.Id);
            return;
        }

        try
        {
            await _rejectionEmailService.SendRejectionEmailAsync(
                new RejectionEmailContext
                {
                    CandidateEmail = candidateUser.Email,
                    CandidateName = candidateUser.FullName,
                    JobTitle = application.JobPosting.JobTitle,
                    RejectionReason = rejectionReason,
                    SkillGaps = ParseJsonArray(application.CVScreeningResult?.MissingSkills),
                    Concerns = ParseJsonArray(application.CVScreeningResult?.Concerns)
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send rejection email for Application {ApplicationId}", application.Id);
        }
    }

    private static string[]? ParseJsonArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<string[]>(json);
        }
        catch
        {
            return null;
        }
    }
}
