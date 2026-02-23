using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.Applications.Commands.ConfirmInterviewSchedule;

/// <summary>
/// Handler for confirming the interview schedule.
/// For Online interviews: saves the provided MeetingLink.
/// For Offline interviews: uses the provided Location.
/// Sends confirmation emails to the Candidate and all Interviewers.
/// Restricted to HRManager.
/// </summary>
public sealed class ConfirmInterviewScheduleHandler : IRequestHandler<ConfirmInterviewScheduleCommand, ConfirmInterviewScheduleResult>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailService _emailService;
    private readonly ILogger<ConfirmInterviewScheduleHandler> _logger;

    public ConfirmInterviewScheduleHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        IEmailService emailService,
        ILogger<ConfirmInterviewScheduleHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _emailService = emailService;
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

        // BEGIN TRANSACTION
        await using var transaction = await _context.BeginTransactionAsync(cancellationToken);

        try
        {
            // 6. Update Interview details
            interview.InterviewFormat = request.InterviewFormat;
            interview.ScheduledAt = request.ScheduledAt;
            interview.Duration = request.Duration;
            interview.Location = request.Location;
            interview.MeetingLink = request.InterviewFormat == InterviewFormat.Online ? request.MeetingLink : null;
            interview.Status = InterviewStatus.Scheduled;
            
            // 7. Update Application stage
            interview.Application.Stage = ApplicationStage.InterviewScheduled;
            interview.Application.StageUpdatedAt = DateTime.UtcNow;
            interview.Application.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Interview {InterviewId} confirmed ({Format}) for Application {ApplicationId} by HR {UserId}. Meeting Link: {Link}, Location: {Location}",
                interview.Id, request.InterviewFormat, interview.ApplicationId, userId, interview.MeetingLink, request.Location);

            // 8. Send confirmation emails (fire-and-forget, after commit)
            await SendConfirmationEmailsAsync(interview);

            return new ConfirmInterviewScheduleResult
            {
                InterviewId = interview.Id,
                Status = interview.Status,
                InterviewFormat = interview.InterviewFormat,
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

    private async Task SendConfirmationEmailsAsync(Domain.Entities.Application.Interview interview)
    {
        var jobTitle = interview.Application.JobPosting.JobTitle;
        var scheduledAt = interview.ScheduledAt.ToString("dddd, MMMM dd, yyyy 'at' hh:mm tt 'UTC'");
        var duration = interview.Duration;
        var format = interview.InterviewFormat == InterviewFormat.Online ? "Online" : "Offline";

        var locationOrLink = interview.InterviewFormat == InterviewFormat.Online
            ? $"<strong>Meeting Link:</strong> <a href=\"{interview.MeetingLink}\">{interview.MeetingLink}</a>"
            : $"<strong>Location:</strong> {interview.Location}";

        var emailBody = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px;'>
                <h2 style='color: #2E86AB;'>Interview Confirmation</h2>
                <p>Your interview has been scheduled with the following details:</p>
                <table style='border-collapse: collapse; width: 100%;'>
                    <tr><td style='padding: 8px; border-bottom: 1px solid #ddd;'><strong>Position:</strong></td><td style='padding: 8px; border-bottom: 1px solid #ddd;'>{jobTitle}</td></tr>
                    <tr><td style='padding: 8px; border-bottom: 1px solid #ddd;'><strong>Date & Time:</strong></td><td style='padding: 8px; border-bottom: 1px solid #ddd;'>{scheduledAt}</td></tr>
                    <tr><td style='padding: 8px; border-bottom: 1px solid #ddd;'><strong>Duration:</strong></td><td style='padding: 8px; border-bottom: 1px solid #ddd;'>{duration} minutes</td></tr>
                    <tr><td style='padding: 8px; border-bottom: 1px solid #ddd;'><strong>Format:</strong></td><td style='padding: 8px; border-bottom: 1px solid #ddd;'>{format}</td></tr>
                    <tr><td style='padding: 8px;' colspan='2'>{locationOrLink}</td></tr>
                </table>
                <p style='margin-top: 20px; color: #666;'>Please ensure you are available at the scheduled time. If you have any questions, please contact the HR department.</p>
            </div>";

        const string subject = "Interview Schedule Confirmation - ERMS";

        // Send to candidate
        var candidateEmail = interview.Application.Candidate.User?.Email;
        if (!string.IsNullOrEmpty(candidateEmail))
        {
            try
            {
                await _emailService.SendEmailAsync(candidateEmail, subject, emailBody);
                _logger.LogInformation("Interview confirmation email sent to candidate: {Email}", candidateEmail);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send interview confirmation email to candidate: {Email}", candidateEmail);
            }
        }

        // Send to each interviewer/participant
        foreach (var participant in interview.Participants)
        {
            var interviewerEmail = participant.Employee?.User?.Email;
            if (string.IsNullOrEmpty(interviewerEmail)) continue;

            try
            {
                await _emailService.SendEmailAsync(interviewerEmail, subject, emailBody);
                _logger.LogInformation("Interview confirmation email sent to interviewer: {Email}", interviewerEmail);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send interview confirmation email to interviewer: {Email}", interviewerEmail);
            }
        }
    }
}
