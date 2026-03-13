using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Interviews.Queries.GetInterviewsForFeedback;

public sealed class GetInterviewsForFeedbackHandler : IRequestHandler<GetInterviewsForFeedbackQuery, GetInterviewsForFeedbackResponse>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetInterviewsForFeedbackHandler(IERMSDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<GetInterviewsForFeedbackResponse> Handle(GetInterviewsForFeedbackQuery request, CancellationToken cancellationToken)
    {
        // 1. Validate current user is authenticated
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

        // 2. Role check: DepartmentHead ONLY
        var userRoles = _currentUserService.Roles;
        if (userRoles == null || !userRoles.Contains(AppRoles.DepartmentHead))
        {
            throw new UnauthorizedAccessException("Chỉ Trưởng phòng mới có quyền xem tổng quan đánh giá phỏng vấn.");
        }

        // 3. Department scoping
        var departmentId = await _currentUserService.GetDepartmentIdAsync()
            ?? throw new UnauthorizedAccessException("Người dùng không thuộc phòng ban nào.");

        // 4. Enterprise scoping
        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync()
            ?? throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào.");

        // 5. Build query
        var query = _context.Interviews
            .Include(i => i.Application)
                .ThenInclude(a => a.JobPosting)
            .Include(i => i.Application)
                .ThenInclude(a => a.Candidate)
                    .ThenInclude(c => c.User)
            .Include(i => i.Participants)
            .Where(i => !i.IsDeleted && !i.Application.IsDeleted)
            .Where(i => i.Application.JobPosting.EnterpriseId == enterpriseId)
            .Where(i => i.Application.JobPosting.DepartmentId == departmentId)
            .Where(i => i.Status == InterviewStatus.Scheduled)
            .Where(i => i.Participants.Any(p => p.FeedbackSubmittedAt != null));

        // 6. Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // 7. Sort by CompletedAt descending, then paginate
        var items = await query
            .OrderByDescending(i => i.CompletedAt)
            .ThenByDescending(i => i.ScheduledAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(i => new InterviewFeedbackSummaryDto
            {
                InterviewId = i.Id,
                ApplicationId = i.ApplicationId,
                CandidateName = i.Application.Candidate.User.FullName,
                CandidateEmail = i.Application.Candidate.User.Email,
                JobTitle = i.Application.JobPosting.JobTitle,
                InterviewType = i.InterviewType,
                RoundNumber = i.RoundNumber,
                CompletedAt = i.CompletedAt,
                FeedbacksReceived = i.Participants.Count(p => p.FeedbackSubmittedAt != null),
                TotalInterviewers = i.Participants.Count,
                DepartmentHeadDecision = i.Decision
            })
            .ToListAsync(cancellationToken);

        return new GetInterviewsForFeedbackResponse
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
