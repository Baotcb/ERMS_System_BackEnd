using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.Applications.Commands.SubmitInterviewFeedback;

/// <summary>
/// Handler for Stage 1: Interviewer submits individual feedback.
/// Updates only the InterviewParticipant record. Does NOT change Interview status or Application stage.
/// </summary>
public sealed class SubmitInterviewFeedbackHandler : IRequestHandler<SubmitInterviewFeedbackCommand, SubmitInterviewFeedbackResult>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<SubmitInterviewFeedbackHandler> _logger;

    public SubmitInterviewFeedbackHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        ILogger<SubmitInterviewFeedbackHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<SubmitInterviewFeedbackResult> Handle(SubmitInterviewFeedbackCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate current user is authenticated
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

        // 2. Resolve EmployeeId from UserId
        var employee = await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.UserId == userId && !e.IsDeleted, cancellationToken)
            ?? throw new UnauthorizedAccessException("Người dùng không phải là nhân viên.");

        // 3. Get enterprise ID
        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync()
            ?? throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào.");

        // 4. Load the interview with participants and application
        var interview = await _context.Interviews
            .Include(i => i.Participants)
            .Include(i => i.Application)
                .ThenInclude(a => a.JobPosting)
            .FirstOrDefaultAsync(i =>
                i.Id == request.InterviewId &&
                i.ApplicationId == request.ApplicationId &&
                !i.IsDeleted,
                cancellationToken)
            ?? throw new Exception($"Không tìm thấy buổi phỏng vấn với ID {request.InterviewId} cho hồ sơ {request.ApplicationId}.");

        // 5. Validate enterprise ownership
        if (interview.Application.JobPosting.EnterpriseId != enterpriseId)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền truy cập hồ sơ ứng tuyển này.");
        }

        // 6. Validate interview is in 'Scheduled' status
        if (!interview.Status.Equals(InterviewStatus.Scheduled, StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception($"Không thể gửi đánh giá. Trạng thái phỏng vấn là '{interview.Status}', yêu cầu '{InterviewStatus.Scheduled}'.");
        }

        // 7. Find the participant record matching the caller's EmployeeId
        var participant = interview.Participants
            .FirstOrDefault(p => p.EmployeeId == employee.Id)
            ?? throw new UnauthorizedAccessException("Bạn không phải là người tham gia buổi phỏng vấn này.");

        // 8. Guard: participant must not have already submitted feedback
        if (participant.FeedbackSubmittedAt != null)
        {
            throw new Exception("Bạn đã gửi đánh giá cho buổi phỏng vấn này rồi.");
        }

        // 9. Update participant feedback fields
        participant.Rating = request.Rating;
        participant.Feedback = request.Feedback.Trim();
        participant.Recommendation = request.Recommendation?.Trim();
        participant.FeedbackSubmittedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Interviewer {EmployeeId} submitted feedback for Interview {InterviewId}, Application {ApplicationId}. Rating: {Rating}",
            employee.Id, request.InterviewId, request.ApplicationId, request.Rating);

        return new SubmitInterviewFeedbackResult
        {
            ParticipantId = participant.Id,
            InterviewId = interview.Id,
            Rating = request.Rating,
            FeedbackSubmittedAt = participant.FeedbackSubmittedAt.Value
        };
    }
}
