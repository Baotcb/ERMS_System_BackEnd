using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.Applications.Commands.ConfirmInterviewSchedule;

/// <summary>
/// Handler for confirming the interview schedule.
/// Generates a Google Meet link and updates status to Scheduled.
/// Restricted to HRManager.
/// </summary>
public sealed class ConfirmInterviewScheduleHandler : IRequestHandler<ConfirmInterviewScheduleCommand, ConfirmInterviewScheduleResult>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IGoogleCalendarService _googleCalendarService;
    private readonly ILogger<ConfirmInterviewScheduleHandler> _logger;

    public ConfirmInterviewScheduleHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        IGoogleCalendarService googleCalendarService,
        ILogger<ConfirmInterviewScheduleHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _googleCalendarService = googleCalendarService;
        _logger = logger;
    }

    public async Task<ConfirmInterviewScheduleResult> Handle(ConfirmInterviewScheduleCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate current user
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        // 2. Role check: HRManager ONLY
        var userRoles = _currentUserService.Roles;
        if (userRoles == null || !userRoles.Contains(AppRoles.HRManager))
        {
            throw new UnauthorizedAccessException("Only HR Manager can confirm interview schedules.");
        }

        // 3. Get enterprise ID
        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync()
            ?? throw new UnauthorizedAccessException("User is not associated with any enterprise.");

        // 4. Find the PENDING interview for this application
        var interview = await _context.Interviews
            .Include(i => i.Application)
                .ThenInclude(a => a.JobPosting)
            .Include(i => i.Application)
                .ThenInclude(a => a.Candidate)
                    .ThenInclude(c => c.User)
            .Include(i => i.Participants)
                .ThenInclude(p => p.Employee)
                    .ThenInclude(e => e.User)
            .FirstOrDefaultAsync(i => 
                i.ApplicationId == request.ApplicationId && 
                i.Status == InterviewStatus.PendingSchedule && 
                !i.IsDeleted, 
                cancellationToken)
            ?? throw new Exception($"No pending interview found for Application {request.ApplicationId}.");

        // 5. Validate enterprise ownership
        if (interview.Application.JobPosting.EnterpriseId != enterpriseId)
        {
            throw new UnauthorizedAccessException("You do not have permission to access this application.");
        }

        // 6. Generate Google Meet Link
        var candidateName = interview.Application.Candidate.User.FullName;
        var jobTitle = interview.Application.JobPosting.JobTitle;
        var title = $"Interview for {jobTitle} - {candidateName}";

        var attendees = interview.Participants
            .Select(p => p.Employee.User.Email)
            .Where(email => !string.IsNullOrEmpty(email))
            .ToList<string>(); // Explicitly cast to List<string> to match interface

        // Add candidate email if available
        if (!string.IsNullOrEmpty(interview.Application.Candidate.User.Email))
        {
            attendees.Add(interview.Application.Candidate.User.Email);
        }

        var meetingLink = await _googleCalendarService.CreateMeetingAsync(
            title, 
            request.ScheduledAt, 
            request.Duration, 
            attendees!);

        // BEGIN TRANSACTION
        await using var transaction = await _context.BeginTransactionAsync(cancellationToken);

        try
        {
            // 7. Update Interview details
            interview.ScheduledAt = request.ScheduledAt;
            interview.Duration = request.Duration;
            interview.Location = request.Location;
            interview.MeetingLink = meetingLink;
            interview.Status = InterviewStatus.Scheduled;
            
            // 8. Update Application stage
            var previousStage = interview.Application.Stage;
            interview.Application.Stage = ApplicationStage.InterviewScheduled;
            interview.Application.StageUpdatedAt = DateTime.UtcNow;
            interview.Application.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Interview {InterviewId} confirmed for Application {ApplicationId} by HR {UserId}. Meeting Link: {Link}",
                interview.Id, interview.ApplicationId, userId, meetingLink);

            return new ConfirmInterviewScheduleResult
            {
                InterviewId = interview.Id,
                Status = interview.Status,
                ScheduledAt = interview.ScheduledAt,
                Duration = interview.Duration,
                MeetingLink = interview.MeetingLink,
                Location = interview.Location
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to confirm interview schedule for Application {ApplicationId}", request.ApplicationId);
            throw;
        }
    }
}
