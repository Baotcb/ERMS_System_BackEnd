using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Application;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.Applications.Commands.AssignInterviewer;

/// <summary>
/// Handler for assigning interviewers to a shortlisted application.
/// Creates an interview with status 'PendingSchedule'.
/// </summary>
public sealed class AssignInterviewerHandler : IRequestHandler<AssignInterviewerCommand, AssignInterviewerResult>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AssignInterviewerHandler> _logger;

    public AssignInterviewerHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        ILogger<AssignInterviewerHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<AssignInterviewerResult> Handle(AssignInterviewerCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate current user is authenticated
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

        // 2. Role check: DepartmentHead ONLY
        var userRoles = _currentUserService.Roles;
        if (userRoles == null || !userRoles.Contains(AppRoles.DepartmentHead))
        {
            throw new UnauthorizedAccessException("Chỉ Trưởng phòng mới có quyền phân công người phỏng vấn.");
        }

        // 3. Get user's department
        var userDepartmentId = await _currentUserService.GetDepartmentIdAsync()
            ?? throw new UnauthorizedAccessException("Người dùng không thuộc phòng ban nào.");

        // 4. Get enterprise ID
        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync()
            ?? throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào.");

        // 5. Load the application with related JobPosting
        var application = await _context.Applications
            .Include(a => a.JobPosting)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId && !a.IsDeleted, cancellationToken)
            ?? throw new Exception($"Không tìm thấy hồ sơ ứng tuyển với ID {request.ApplicationId}.");

        // 6. Validate enterprise ownership
        if (application.JobPosting.EnterpriseId != enterpriseId)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền truy cập hồ sơ ứng tuyển này.");
        }

        // 7. Department security check
        if (application.JobPosting.DepartmentId != userDepartmentId)
        {
            throw new UnauthorizedAccessException("Bạn chỉ có thể phân công người phỏng vấn cho ứng viên trong phòng ban mình.");
        }

        // 8. Validate current stage is "Shortlisted"
        if (!application.Stage.Equals(ApplicationStage.Shortlisted, StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception($"Không thể phân công người phỏng vấn. Giai đoạn hồ sơ là '{application.Stage}', yêu cầu '{ApplicationStage.Shortlisted}'.");
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
            throw new Exception($"Một số người phỏng vấn không được tìm thấy: {string.Join(", ", missingIds)}");
        }

        // BEGIN TRANSACTION
        // BEGIN TRANSACTION
        await using var transaction = await _context.BeginTransactionAsync(cancellationToken);

        try
        {
            // 10. Create Interview record with PendingSchedule status
            var interview = new Interview
            {
                Id = Guid.CreateVersion7(),
                ApplicationId = application.Id,
                InterviewType = request.InterviewType,
                RoundNumber = 1, // Defaulting to 1 for now, logic could be enhanced for multi-round
                Status = InterviewStatus.PendingSchedule,
                ScheduledById = userId,
                Note = request.Note?.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            // 11. Create InterviewParticipant records logic integrated into Interview creation via navigation
            foreach (var employeeId in request.InterviewerIds)
            {
                var participant = new InterviewParticipant
                {
                    Id = Guid.CreateVersion7(),
                    EmployeeId = employeeId,
                    Role = InterviewParticipantRoles.Interviewer,
                    IsRequired = true,
                    ConfirmationStatus = "Pending",
                    CreatedAt = DateTime.UtcNow
                };
                
                // Add to navigation property
                interview.Participants.Add(participant);
            }

            // Add the Interview (and its graph) to the context
            _context.Interviews.Add(interview);

            // NOTE: Do NOT update Application.Stage here. It remains Shortlisted until HR schedules it.

            await _context.SaveChangesAsync(cancellationToken);

            // COMMIT TRANSACTION
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Interview {InterviewId} assigned for Application {ApplicationId} by user {UserId}. Status: {Status}",
                interview.Id, application.Id, userId, interview.Status);

            // Build participant DTOs with employee names
            var participantDtos = interviewerEmployees
                .Select(e => new InterviewParticipantDto
                {
                    ParticipantId = interview.Participants.First(p => p.EmployeeId == e.Id).Id,
                    EmployeeId = e.Id,
                    EmployeeName = e.User.FullName,
                    Role = InterviewParticipantRoles.Interviewer,
                    ConfirmationStatus = "Pending"
                })
                .ToList();

            return new AssignInterviewerResult
            {
                InterviewId = interview.Id,
                ApplicationId = application.Id,
                InterviewType = interview.InterviewType,
                Status = interview.Status,
                Participants = participantDtos
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to assign interviewers for Application {ApplicationId}", request.ApplicationId);
            throw;
        }
    }
}
