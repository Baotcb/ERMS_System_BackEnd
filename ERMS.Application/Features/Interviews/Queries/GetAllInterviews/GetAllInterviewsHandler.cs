using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Interviews.Queries.GetAllInterviews;

/// <summary>
/// Handler for retrieving all interviews across the enterprise
/// </summary>
public sealed class GetAllInterviewsHandler : IRequestHandler<GetAllInterviewsQuery, GetAllInterviewsResponse>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetAllInterviewsHandler(IERMSDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<GetAllInterviewsResponse> Handle(GetAllInterviewsQuery request, CancellationToken cancellationToken)
    {
        // 1. Validate current user is authenticated
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        // 2. Validate user has appropriate HR/Director roles
        var userRoles = _currentUserService.Roles;
        if (userRoles == null || (!userRoles.Contains(AppRoles.HRManager) && !userRoles.Contains(AppRoles.Director)))
        {
            throw new UnauthorizedAccessException("Only HR Managers or Directors can view all interviews.");
        }

        // 3. Enterprise scoping
        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync()
            ?? throw new UnauthorizedAccessException("User is not associated with any enterprise.");

        // 4. Build query: All interviews linked to a JobPosting within the user's Enterprise
        var query = _context.Interviews
            .Include(i => i.Application)
                .ThenInclude(a => a.JobPosting)
            .Include(i => i.Application)
                .ThenInclude(a => a.Candidate)
                    .ThenInclude(c => c.User)
            .Include(i => i.ScheduledBy)
            .Include(i => i.Participants)
                .ThenInclude(p => p.Employee)
                    .ThenInclude(e => e.User)
            .Where(i => !i.IsDeleted && !i.Application.IsDeleted)
            .Where(i => i.Application.JobPosting.EnterpriseId == enterpriseId);

        // 5. Optional status filter
        if (!string.IsNullOrWhiteSpace(request.StatusFilter))
        {
            query = query.Where(i => i.Status == request.StatusFilter);
        }

        // 6. Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // 7. Sort by ScheduledAt descending (most recent first, nulls last), then paginate
        var items = await query
            .OrderByDescending(i => i.ScheduledAt)
            .ThenByDescending(i => i.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(i => new InterviewDto
            {
                InterviewId = i.Id,
                ApplicationId = i.ApplicationId,
                CandidateName = i.Application.Candidate.User.FullName,
                CandidateEmail = i.Application.Candidate.User.Email,
                JobTitle = i.Application.JobPosting.JobTitle,
                InterviewType = i.InterviewType,
                InterviewFormat = i.InterviewFormat.ToString(),
                RoundNumber = i.RoundNumber,
                ScheduledAt = i.ScheduledAt,
                Duration = i.Duration,
                Location = i.Location,
                MeetingLink = i.MeetingLink,
                Status = i.Status,
                ScheduledById = i.ScheduledById,
                ScheduledByName = i.ScheduledBy != null ? i.ScheduledBy.FullName : string.Empty,
                Participants = i.Participants.Select(p => new InterviewParticipantSummaryDto
                {
                    ParticipantId = p.Id,
                    EmployeeId = p.EmployeeId,
                    EmployeeName = p.Employee.User.FullName,
                    Role = p.Role,
                    ConfirmationStatus = p.ConfirmationStatus,
                    HasSubmittedFeedback = p.FeedbackSubmittedAt.HasValue
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        return new GetAllInterviewsResponse
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
