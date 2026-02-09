using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Application;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.Applications.Commands.ScheduleInterview;

/// <summary>
/// Handler for scheduling an interview for a shortlisted application
/// Uses transaction to ensure atomicity of Interview, InterviewParticipants, and Application stage update
/// </summary>
public sealed class ScheduleInterviewHandler : IRequestHandler<ScheduleInterviewCommand, ScheduleInterviewResult>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ScheduleInterviewHandler> _logger;

    public ScheduleInterviewHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        ILogger<ScheduleInterviewHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ScheduleInterviewResult> Handle(ScheduleInterviewCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate current user is authenticated
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        // 2. Role check: DepartmentHead ONLY
        var userRoles = _currentUserService.Roles;
        if (userRoles == null || !userRoles.Contains(AppRoles.DepartmentHead))
        {
            throw new UnauthorizedAccessException("Only Department Head can schedule interviews.");
        }

        // 3. Get user's department
        var userDepartmentId = await _currentUserService.GetDepartmentIdAsync()
            ?? throw new UnauthorizedAccessException("User is not associated with any department.");

        // 4. Get enterprise ID
        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync()
            ?? throw new UnauthorizedAccessException("User is not associated with any enterprise.");

        // 5. Load the application with related JobPosting
        var application = await _context.Applications
            .Include(a => a.JobPosting)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId && !a.IsDeleted, cancellationToken)
            ?? throw new Exception($"Application with ID {request.ApplicationId} not found.");

        // 6. Validate enterprise ownership
        if (application.JobPosting.EnterpriseId != enterpriseId)
        {
            throw new UnauthorizedAccessException("You do not have permission to access this application.");
        }

        // 7. Department security check
        if (application.JobPosting.DepartmentId != userDepartmentId)
        {
            throw new UnauthorizedAccessException("You can only schedule interviews for candidates in your department.");
        }

        // 8. Validate current stage is "Shortlisted"
        if (!application.Stage.Equals(ApplicationStage.Shortlisted, StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception($"Cannot schedule interview. Application stage is '{application.Stage}', expected '{ApplicationStage.Shortlisted}'.");
        }

        // 9. Validate interviewers exist and belong to enterprise
        var interviewerEmployees = await _context.Employees
            .Where(e => request.InterviewerIds.Contains(e.Id) && e.EnterpriseId == enterpriseId && !e.IsDeleted)
            .Include(e => e.User)
            .ToListAsync(cancellationToken);

        if (interviewerEmployees.Count != request.InterviewerIds.Count)
        {
            var foundIds = interviewerEmployees.Select(e => e.Id).ToHashSet();
            var missingIds = request.InterviewerIds.Where(id => !foundIds.Contains(id)).ToList();
            throw new Exception($"Some interviewers were not found: {string.Join(", ", missingIds)}");
        }

        // Store previous stage for response
        var previousStage = application.Stage;

        // BEGIN TRANSACTION
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 10. Create Interview record
            var interview = new Interview
            {
                Id = Guid.CreateVersion7(),
                ApplicationId = application.Id,
                InterviewType = request.InterviewType,
                RoundNumber = 1,
                ScheduledAt = request.ScheduledAt,
                Duration = request.Duration,
                Location = request.Location?.Trim(),
                MeetingLink = request.MeetingLink?.Trim(),
                Status = "Scheduled",
                ScheduledById = userId,
                Note = request.Note?.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Interviews.Add(interview);

            // 11. Create InterviewParticipant records
            var participants = new List<InterviewParticipant>();
            foreach (var employeeId in request.InterviewerIds)
            {
                var participant = new InterviewParticipant
                {
                    Id = Guid.CreateVersion7(),
                    InterviewId = interview.Id,
                    EmployeeId = employeeId,
                    Role = "Interviewer",
                    IsRequired = true,
                    ConfirmationStatus = "Pending",
                    CreatedAt = DateTime.UtcNow
                };
                participants.Add(participant);
                _context.InterviewParticipants.Add(participant);
            }

            // 12. Update Application stage to InterviewScheduled
            application.Stage = ApplicationStage.InterviewScheduled;
            application.StageUpdatedAt = DateTime.UtcNow;
            application.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // COMMIT TRANSACTION
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Interview {InterviewId} scheduled for Application {ApplicationId} by user {UserId}. Stage changed from {PreviousStage} to {NewStage}",
                interview.Id, application.Id, userId, previousStage, application.Stage);

            // Build participant DTOs with employee names
            var participantDtos = interviewerEmployees
                .Select(e => new InterviewParticipantDto
                {
                    ParticipantId = participants.First(p => p.EmployeeId == e.Id).Id,
                    EmployeeId = e.Id,
                    EmployeeName = e.User.FullName,
                    Role = "Interviewer",
                    ConfirmationStatus = "Pending"
                })
                .ToList();

            return new ScheduleInterviewResult
            {
                InterviewId = interview.Id,
                ApplicationId = application.Id,
                PreviousStage = previousStage,
                NewStage = application.Stage,
                ScheduledAt = interview.ScheduledAt,
                Duration = interview.Duration,
                InterviewType = interview.InterviewType,
                Location = interview.Location,
                MeetingLink = interview.MeetingLink,
                RoundNumber = interview.RoundNumber,
                Participants = participantDtos
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to schedule interview for Application {ApplicationId}", request.ApplicationId);
            throw;
        }
    }
}
