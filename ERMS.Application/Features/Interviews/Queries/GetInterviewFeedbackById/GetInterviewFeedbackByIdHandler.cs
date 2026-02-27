using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Interviews.Queries.GetInterviewFeedbackById;

public sealed class GetInterviewFeedbackByIdHandler : IRequestHandler<GetInterviewFeedbackByIdQuery, GetInterviewFeedbackByIdResponse>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetInterviewFeedbackByIdHandler(IERMSDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<GetInterviewFeedbackByIdResponse> Handle(GetInterviewFeedbackByIdQuery request, CancellationToken cancellationToken)
    {
        // 1. Validate current user is authenticated
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        // 2. Role check: DepartmentHead ONLY
        var userRoles = _currentUserService.Roles;
        if (userRoles == null || !userRoles.Contains(AppRoles.DepartmentHead))
        {
            throw new UnauthorizedAccessException("Only Department Heads can view detailed interview feedback.");
        }

        // 3. Department scoping
        var departmentId = await _currentUserService.GetDepartmentIdAsync()
            ?? throw new UnauthorizedAccessException("User is not associated with any department.");

        // 4. Enterprise scoping
        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync()
            ?? throw new UnauthorizedAccessException("User is not associated with any enterprise.");

        // 5. Query the precise interview
        var interview = await _context.Interviews
            .Include(i => i.Application)
                .ThenInclude(a => a.JobPosting)
            .Include(i => i.Application)
                .ThenInclude(a => a.Candidate)
                    .ThenInclude(c => c.User)
            .Include(i => i.Participants)
                .ThenInclude(p => p.Employee)
                    .ThenInclude(e => e.User)
            .Where(i => i.Id == request.InterviewId)
            .Where(i => !i.IsDeleted && !i.Application.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new Exception($"Completed interview with ID {request.InterviewId} not found.");

        // 6. Security & State Guard Clauses
        if (interview.Application.JobPosting.EnterpriseId != enterpriseId)
        {
            throw new UnauthorizedAccessException("You do not have permission to access this enterprise's data.");
        }

        if (interview.Application.JobPosting.DepartmentId != departmentId)
        {
            throw new UnauthorizedAccessException("You do not have permission to access interviews from another department.");
        }

        if (interview.Status != InterviewStatus.Completed)
        {
            throw new Exception("Feedback review is only available for completed interviews.");
        }

        if (!interview.Participants.Any(p => p.FeedbackSubmittedAt != null))
        {
            throw new Exception("This interview has not received any feedback yet.");
        }

        // 7. Map to Detailed DTO
        return new GetInterviewFeedbackByIdResponse
        {
            InterviewId = interview.Id,
            ApplicationId = interview.ApplicationId,
            CandidateName = interview.Application.Candidate.User.FullName,
            CandidateEmail = interview.Application.Candidate.User.Email,
            JobTitle = interview.Application.JobPosting.JobTitle,
            DepartmentId = interview.Application.JobPosting.DepartmentId,
            InterviewType = interview.InterviewType,
            RoundNumber = interview.RoundNumber,
            CompletedAt = interview.CompletedAt,
            
            // Dept Head fields
            DepartmentHeadDecision = interview.Decision,
            DepartmentHeadOverallRating = interview.OverallRating,
            DepartmentHeadOverallFeedback = interview.OverallFeedback,
            DepartmentHeadNote = interview.Note,

            // Participants array
            ParticipantsFeedback = interview.Participants
                .Where(p => p.FeedbackSubmittedAt != null)
                .Select(p => new ParticipantFeedbackDto
                {
                    ParticipantId = p.Id,
                    EmployeeName = p.Employee.User.FullName,
                    Role = p.Role,
                    Rating = p.Rating,
                    Feedback = p.Feedback,
                    Recommendation = p.Recommendation,
                    FeedbackSubmittedAt = p.FeedbackSubmittedAt
                })
                .OrderByDescending(p => p.FeedbackSubmittedAt)
                .ToList()
        };
    }
}
