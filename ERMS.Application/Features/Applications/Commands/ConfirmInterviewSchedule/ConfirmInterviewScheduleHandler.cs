using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.Applications.Commands.ConfirmInterviewSchedule;

public sealed class ConfirmInterviewScheduleHandler : IRequestHandler<ConfirmInterviewScheduleCommand, ConfirmInterviewScheduleResult>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailService _emailService;
    private readonly IZoomService _zoomService;
    private readonly ICalendarService _calendarService;
    private readonly ILogger<ConfirmInterviewScheduleHandler> _logger;

    public ConfirmInterviewScheduleHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        IEmailService emailService,
        IZoomService zoomService,
        ICalendarService calendarService,
        ILogger<ConfirmInterviewScheduleHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _emailService = emailService;
        _zoomService = zoomService;
        _calendarService = calendarService;
        _logger = logger;
    }

    public async Task<ConfirmInterviewScheduleResult> Handle(ConfirmInterviewScheduleCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate current user
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

        // 2. Role check: HRManager ONLY
        var userRoles = _currentUserService.Roles;
        if (userRoles == null || !userRoles.Contains(AppRoles.HRManager))
        {
            throw new UnauthorizedAccessException("Chỉ HR Manager mới có quyền xác nhận lịch phỏng vấn.");
        }

        // 3. Get enterprise ID
        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync()
            ?? throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào.");

        // 4. Find the PENDING interview for this application
        var interview = await _context.Interviews
            .Include(i => i.Application)
                .ThenInclude(a => a.JobPosting)
                    .ThenInclude(jp => jp.Enterprise)  
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
            ?? throw new Exception($"Không tìm thấy buổi phỏng vấn đang chờ cho hồ sơ {request.ApplicationId}.");

        // 5. Validate enterprise ownership
        if (interview.Application.JobPosting.EnterpriseId != enterpriseId)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền truy cập hồ sơ ứng tuyển này.");
        }

        // 6. Auto-create Zoom meeting if needed
        string? meetingLink = request.MeetingLink;
        if (request.InterviewFormat == InterviewFormat.Online && string.IsNullOrWhiteSpace(meetingLink))
        {
            try
            {
                var candidateFullName = interview.Application.Candidate.User?.FullName ?? "Candidate";
                var zoomMeeting = await _zoomService.CreateMeetingAsync(new ZoomMeetingRequest
                {
                    Topic = $"Interview for {interview.Application.JobPosting.JobTitle}",
                    StartTime = request.ScheduledAt,
                    Duration = request.Duration,
                    Timezone = "UTC",
                    Agenda = $"Interview with candidate {candidateFullName}"
                }, cancellationToken);

                meetingLink = zoomMeeting.JoinUrl;
                _logger.LogInformation("Đã tự động tạo cuộc họ p Zoom {MeetingId} cho cuộc phỏng vấn {InterviewId}", 
                    zoomMeeting.MeetingId, interview.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể tự động tạo cuộc họ p Zoom cho cuộc phỏng vấn {InterviewId}", interview.Id);
                throw new Exception("Không thể tạo cuộc họp Zoom. Vui lòng thử lại hoặc cung cấp link họp thủ công.", ex);
            }
        }

        await using var transaction = await _context.BeginTransactionAsync(cancellationToken);

        try
        {
            // 7. Update Interview details
            interview.InterviewFormat = request.InterviewFormat;
            interview.ScheduledAt = request.ScheduledAt;
            interview.Duration = request.Duration;
            interview.Location = request.Location;
            interview.MeetingLink = request.InterviewFormat == InterviewFormat.Online ? meetingLink : null;
            interview.Status = InterviewStatus.Scheduled;
            
            // 8. Update Application stage
            interview.Application.Stage = ApplicationStage.InterviewScheduled;
            interview.Application.StageUpdatedAt = DateTime.UtcNow;
            interview.Application.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Interview {InterviewId} confirmed ({Format}) for Application {ApplicationId} by HR {UserId}. Meeting Link: {Link}, Location: {Location}",
                interview.Id, request.InterviewFormat, interview.ApplicationId, userId, interview.MeetingLink, request.Location);

            // 9. Send confirmation emails with calendar invite (fire-and-forget, after commit)
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
            _logger.LogError(ex, "Không thể xác nhận lịch phỏng vấn cho hồ sơ {ApplicationId}", request.ApplicationId);
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
                <p style='margin-top: 10px; color: #999; font-size: 12px;'>A calendar invitation (.ics file) is attached to this email. Click on it to add this event to your calendar.</p>
            </div>";

        const string subject = "Interview Schedule Confirmation";

        // Create calendar event
        var attendees = new List<string>();
        var candidateEmail = interview.Application.Candidate.User?.Email;
        if (!string.IsNullOrEmpty(candidateEmail))
        {
            attendees.Add(candidateEmail);
        }

        foreach (var participant in interview.Participants)
        {
            var interviewerEmail = participant.Employee?.User?.Email;
            if (!string.IsNullOrEmpty(interviewerEmail))
            {
                attendees.Add(interviewerEmail);
            }
        }

        var calendarDescription = $"Interview for {jobTitle}\n\n";
        if (interview.InterviewFormat == InterviewFormat.Online)
        {
            calendarDescription += $"Join meeting: {interview.MeetingLink}";
        }
        else
        {
            calendarDescription += $"Location: {interview.Location}";
        }
        
        var icsContent = _calendarService.CreateICalendarEvent(new CalendarEventRequest
        {
            Subject = $"Interview: {jobTitle}",
            Description = calendarDescription,
            Location = interview.InterviewFormat == InterviewFormat.Online
                ? interview.MeetingLink ?? "Online"
                : interview.Location ?? "TBD",
            StartTime = interview.ScheduledAt,
            DurationMinutes = interview.Duration,
            AttendeeEmails = attendees,
            OrganizerEmail = _currentUserService.Email ?? "hr@erms.com",
            OrganizerName = interview.Application.JobPosting.Enterprise?.EnterpriseName ?? "ERMS HR Department"  
        });

        var attachments = new Dictionary<string, byte[]>
        {
            { "interview_invite.ics", System.Text.Encoding.UTF8.GetBytes(icsContent) }
        };

        // Send to candidate
        if (!string.IsNullOrEmpty(candidateEmail))
        {
            try
            {
                await _emailService.SendEmailWithAttachmentAsync(candidateEmail, subject, emailBody, attachments);
                _logger.LogInformation("Đã gửi email xác nhận phỏng vấn kèm lời mời lịch cho ứng viên: {Email}", candidateEmail);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể gửi email xác nhận phỏng vấn cho ứng viên: {Email}", candidateEmail);
            }
        }

        // Send to each interviewer/participant
        foreach (var participant in interview.Participants)
        {
            var interviewerEmail = participant.Employee?.User?.Email;
            if (string.IsNullOrEmpty(interviewerEmail)) continue;

            try
            {
                await _emailService.SendEmailWithAttachmentAsync(interviewerEmail, subject, emailBody, attachments);
                _logger.LogInformation("Đã gửi email xác nhận phỏng vấn kèm lời mời lịch cho người phỏng vấn: {Email}", interviewerEmail);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể gửi email xác nhận phỏng vấn cho người phỏng vấn: {Email}", interviewerEmail);
            }
        }
    }
}
