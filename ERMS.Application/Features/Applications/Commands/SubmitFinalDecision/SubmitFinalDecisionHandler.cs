using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Application;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.Applications.Commands.SubmitFinalDecision;

/// <summary>
/// Handler for Stage 2: Department Head submits the final decision on an interview.
/// Updates Interview status to Completed and triggers the workflow branch:
/// - Fail → Application.Stage = Rejected
/// - Passed → Application.Stage = OfferProcessing
/// - NextRound → Creates new Interview with Round + 1
/// </summary>
public sealed class SubmitFinalDecisionHandler : IRequestHandler<SubmitFinalDecisionCommand, SubmitFinalDecisionResult>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<SubmitFinalDecisionHandler> _logger;

    public SubmitFinalDecisionHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        ILogger<SubmitFinalDecisionHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<SubmitFinalDecisionResult> Handle(SubmitFinalDecisionCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate current user is authenticated
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        // 2. Role check: DepartmentHead ONLY
        var userRoles = _currentUserService.Roles;
        if (userRoles == null || !userRoles.Contains(AppRoles.DepartmentHead))
        {
            throw new UnauthorizedAccessException("Only Department Head can submit the final interview decision.");
        }

        // 3. Get user's department
        var userDepartmentId = await _currentUserService.GetDepartmentIdAsync()
            ?? throw new UnauthorizedAccessException("User is not associated with any department.");

        // 4. Get enterprise ID
        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync()
            ?? throw new UnauthorizedAccessException("User is not associated with any enterprise.");

        // 5. Load the interview with Application, JobPosting, and Participants
        var interview = await _context.Interviews
            .Include(i => i.Application)
                .ThenInclude(a => a.JobPosting)
            .Include(i => i.Participants)
            .FirstOrDefaultAsync(i =>
                i.Id == request.InterviewId &&
                i.ApplicationId == request.ApplicationId &&
                !i.IsDeleted,
                cancellationToken)
            ?? throw new Exception($"Interview with ID {request.InterviewId} not found for Application {request.ApplicationId}.");

        // 6. Validate enterprise ownership
        if (interview.Application.JobPosting.EnterpriseId != enterpriseId)
        {
            throw new UnauthorizedAccessException("You do not have permission to access this application.");
        }

        // 7. Department security check
        if (interview.Application.JobPosting.DepartmentId != userDepartmentId)
        {
            throw new UnauthorizedAccessException("You can only make decisions for interviews in your department.");
        }

        // 8. Validate interview is in 'Scheduled' status
        if (!interview.Status.Equals(InterviewStatus.Scheduled, StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception($"Cannot submit decision. Interview status is '{interview.Status}', expected '{InterviewStatus.Scheduled}'.");
        }

        // BEGIN TRANSACTION
        await using var transaction = await _context.BeginTransactionAsync(cancellationToken);

        try
        {
            // 9. Update Interview fields
            interview.Decision = request.Decision;
            interview.OverallRating = request.OverallRating;
            interview.OverallFeedback = request.OverallFeedback?.Trim();
            interview.Note = request.Note?.Trim();
            interview.CompletedAt = DateTime.UtcNow;
            interview.Status = InterviewStatus.Completed;
            interview.UpdatedAt = DateTime.UtcNow;

            // 10. Workflow branching
            Guid? newInterviewId = null;
            var application = interview.Application;

            switch (request.Decision)
            {
                case InterviewDecision.Fail:
                    application.Stage = ApplicationStage.Rejected;
                    application.RejectedAt = DateTime.UtcNow;
                    application.RejectedById = userId;
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

                    // Determine which interviewers to use
                    var customInterviewersProvided = request.NextRoundInterviewerIds != null && request.NextRoundInterviewerIds.Count > 0;
                    var interviewerIdsToUse = customInterviewersProvided
                        ? request.NextRoundInterviewerIds!
                        : interview.Participants.Select(p => p.EmployeeId).ToList();

                    if (customInterviewersProvided)
                    {
                        // Validate the newly provided interviewers exist and belong to the same enterprise
                        var validEmployeeIds = await _context.Employees
                            .Where(e => request.NextRoundInterviewerIds!.Contains(e.Id) && e.EnterpriseId == enterpriseId && !e.IsDeleted)
                            .Select(e => e.Id)
                            .ToListAsync(cancellationToken);

                        if (validEmployeeIds.Count != request.NextRoundInterviewerIds!.Count)
                        {
                            var missingIds = request.NextRoundInterviewerIds.Where(id => !validEmployeeIds.Contains(id)).ToList();
                            throw new Exception($"Some new interviewers were not found or do not belong to your enterprise: {string.Join(", ", missingIds)}");
                        }
                    }

                    // Assign participants
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

            _logger.LogInformation(
                "Final decision '{Decision}' submitted for Interview {InterviewId}, Application {ApplicationId} by DeptHead {UserId}. New Stage: {Stage}",
                request.Decision, interview.Id, application.Id, userId, application.Stage);

            return new SubmitFinalDecisionResult
            {
                InterviewId = interview.Id,
                Decision = request.Decision,
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
}
