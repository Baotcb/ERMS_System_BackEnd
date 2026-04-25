using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Interviews.Queries.GetMyInterviews;

/// <summary>
/// Handler for retrieving the current user's assigned interviews
/// </summary>
public sealed class GetMyInterviewsHandler : IRequestHandler<GetMyInterviewsQuery, GetMyInterviewsResponse>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMyInterviewsHandler(IERMSDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<GetMyInterviewsResponse> Handle(GetMyInterviewsQuery request, CancellationToken cancellationToken)
    {
        // 1. Validate current user is authenticated
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

        // 2. Enterprise scoping
        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync()
            ?? throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào.");

        // 3. Resolve Employee record for the current user
        var employee = await _context.Employees
            .Where(e => e.UserId == userId && !e.IsDeleted)
            .Select(e => new { e.Id })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new UnauthorizedAccessException("Người dùng không phải là nhân viên.");

        // 4. Build query: InterviewParticipants where EmployeeId matches
        var query = _context.InterviewParticipants
            .Where(p => p.EmployeeId == employee.Id)
            .Where(p => !p.Interview.IsDeleted && !p.Interview.Application.IsDeleted)
            .Where(p => p.Interview.Application.JobPosting.EnterpriseId == enterpriseId);

        // 5. Optional status filter
        if (!string.IsNullOrWhiteSpace(request.StatusFilter))
        {
            query = query.Where(p => p.Interview.Status == request.StatusFilter);
        }

        // 6. Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // 7. Sort by ScheduledAt descending (most recent first), then paginate
        var items = await query
            .OrderByDescending(p => p.Interview.ScheduledAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new MyInterviewDto
            {
                InterviewId = p.Interview.Id,
                ApplicationId = p.Interview.ApplicationId,
                CandidateName = p.Interview.Application.Candidate.User.FullName,
                CandidateEmail = p.Interview.Application.Candidate.User.Email,
                JobTitle = p.Interview.Application.JobPosting.JobTitle,
                InterviewType = p.Interview.InterviewType,
                InterviewFormat = p.Interview.InterviewFormat.ToString(),
                RoundNumber = p.Interview.RoundNumber,
                ScheduledAt = p.Interview.ScheduledAt == DateTime.MinValue ? (DateTime?)null : p.Interview.ScheduledAt,
                Duration = p.Interview.Duration,
                Location = p.Interview.Location,
                MeetingLink = p.Interview.MeetingLink,
                Status = p.Interview.Status,
                MyRole = p.Role,
                MyConfirmationStatus = p.ConfirmationStatus,
                HasSubmittedFeedback = p.FeedbackSubmittedAt != null
            })
            .ToListAsync(cancellationToken);

        return new GetMyInterviewsResponse
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
