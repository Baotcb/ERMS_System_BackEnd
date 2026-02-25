using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Applications.Queries.GetShortlistedApplications;

/// <summary>
/// Handler for retrieving shortlisted applications for a job posting
/// Only Department Heads can access candidates for job postings in their department
/// </summary>
public sealed class GetShortlistedApplicationsHandler : IRequestHandler<GetShortlistedApplicationsQuery, GetShortlistedApplicationsResponse>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetShortlistedApplicationsHandler(IERMSDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<GetShortlistedApplicationsResponse> Handle(GetShortlistedApplicationsQuery request, CancellationToken cancellationToken)
    {
        // 1. Validate current user is authenticated
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        // 2. Role check: DepartmentHead ONLY
        var userRoles = _currentUserService.Roles;
        if (userRoles == null || !userRoles.Contains(AppRoles.DepartmentHead))
        {
            throw new UnauthorizedAccessException("Only Department Head can view shortlisted candidates.");
        }

        // 3. Get user's department
        var userDepartmentId = await _currentUserService.GetDepartmentIdAsync()
            ?? throw new UnauthorizedAccessException("User is not associated with any department.");

        // 4. Get enterprise ID for scoping
        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync()
            ?? throw new UnauthorizedAccessException("User is not associated with any enterprise.");

        // 5. Validate job posting exists and get department
        var jobPosting = await _context.JobPostings
            .Where(jp => jp.Id == request.JobPostingId && jp.EnterpriseId == enterpriseId && !jp.IsDeleted)
            .Select(jp => new { jp.Id, jp.JobTitle, jp.DepartmentId })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new Exception($"Job posting with ID {request.JobPostingId} not found.");

        // 6. Department security check: User must belong to same department as job posting
        if (jobPosting.DepartmentId != userDepartmentId)
        {
            throw new UnauthorizedAccessException("You can only view candidates for job postings in your department.");
        }

        // 7. Build query for shortlisted applications only
        var query = _context.Applications
            .Include(a => a.Candidate)
                .ThenInclude(c => c.User)
            .Include(a => a.Resume)
            .Include(a => a.CVScreeningResult)
            .Where(a => a.JobPostingId == request.JobPostingId 
                     && a.Stage == ApplicationStage.Shortlisted 
                     && !a.IsDeleted);

        // 8. Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // 9. Sort by CV Score descending (highest first), then by AppliedAt
        var items = await query
            .OrderByDescending(a => a.CVScreeningResult != null ? a.CVScreeningResult.OverallScore : 0)
            .ThenByDescending(a => a.AppliedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new ShortlistedApplicationDto
            {
                ApplicationId = a.Id,
                CandidateId = a.CandidateId,
                CandidateName = a.Candidate.User.FullName,
                CandidateEmail = a.Candidate.User.Email,
                CandidatePhone = a.Candidate.User.PhoneNumber,
                ResumeUrl = a.Resume != null ? a.Resume.FileUrl : null,
                Stage = a.Stage,
                AppliedAt = a.AppliedAt,
                HRNote = a.HRNote,
                OverallScore = a.CVScreeningResult != null ? a.CVScreeningResult.OverallScore : null,
                SkillMatchScore = a.CVScreeningResult != null ? a.CVScreeningResult.SkillMatchScore : null,
                ExperienceMatchScore = a.CVScreeningResult != null ? a.CVScreeningResult.ExperienceMatchScore : null,
                EducationMatchScore = a.CVScreeningResult != null ? a.CVScreeningResult.EducationMatchScore : null,
                AISummary = a.CVScreeningResult != null ? a.CVScreeningResult.Summary : null,
                MatchedSkills = a.CVScreeningResult != null ? a.CVScreeningResult.MatchedSkills : null,
                MissingSkills = a.CVScreeningResult != null ? a.CVScreeningResult.MissingSkills : null
            })
            .ToListAsync(cancellationToken);

        return new GetShortlistedApplicationsResponse
        {
            JobPostingId = jobPosting.Id,
            JobTitle = jobPosting.JobTitle,
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
