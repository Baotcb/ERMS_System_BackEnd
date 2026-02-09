using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Applications.Queries.GetApplicationsByJob;

/// <summary>
/// Handler for retrieving applications for a specific job posting, sorted by AI match score
/// </summary>
public sealed class GetApplicationsByJobHandler : IRequestHandler<GetApplicationsByJobQuery, GetApplicationsByJobResponse>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetApplicationsByJobHandler(IERMSDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<GetApplicationsByJobResponse> Handle(GetApplicationsByJobQuery request, CancellationToken cancellationToken)
    {
        // 1. Validate current user is authenticated
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        // 2. Role check: HRManager or Director only
        var userRoles = _currentUserService.Roles;
        if (userRoles == null || (!userRoles.Contains(AppRoles.HRManager) && !userRoles.Contains(AppRoles.Director)))
        {
            throw new UnauthorizedAccessException("Only HR Manager or Director can view applications.");
        }

        // 3. Enterprise scoping
        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync()
            ?? throw new UnauthorizedAccessException("User is not associated with any enterprise.");

        // 4. Validate job posting exists and belongs to enterprise
        var jobPosting = await _context.JobPostings
            .Where(jp => jp.Id == request.JobPostingId && jp.EnterpriseId == enterpriseId && !jp.IsDeleted)
            .Select(jp => new { jp.Id, jp.JobTitle })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new Exception($"Job posting with ID {request.JobPostingId} not found.");

        // 5. Build query for applications
        var query = _context.Applications
            .Include(a => a.Candidate)
                .ThenInclude(c => c.User)
            .Include(a => a.Resume)
            .Include(a => a.CVScreeningResult)
            .Where(a => a.JobPostingId == request.JobPostingId && !a.IsDeleted);

        // 6. Optional stage filter
        if (!string.IsNullOrWhiteSpace(request.StageFilter))
        {
            query = query.Where(a => a.Stage == request.StageFilter);
        }

        // 7. Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // 8. Sort by CV Score descending (highest first), then by AppliedAt
        var items = await query
            .OrderByDescending(a => a.CVScreeningResult != null ? a.CVScreeningResult.OverallScore : 0)
            .ThenByDescending(a => a.AppliedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new ApplicationListDto
            {
                ApplicationId = a.Id,
                CandidateId = a.CandidateId,
                CandidateName = a.Candidate.User.FullName,
                CandidateEmail = a.Candidate.User.Email,
                CandidatePhone = a.Candidate.User.PhoneNumber,
                ResumeUrl = a.Resume != null ? a.Resume.FileUrl : null,
                Stage = a.Stage,
                Status = a.Status,
                AppliedAt = a.AppliedAt,
                HRNote = a.HRNote,
                OverallScore = a.CVScreeningResult != null ? a.CVScreeningResult.OverallScore : null,
                SkillMatchScore = a.CVScreeningResult != null ? a.CVScreeningResult.SkillMatchScore : null,
                ExperienceMatchScore = a.CVScreeningResult != null ? a.CVScreeningResult.ExperienceMatchScore : null,
                AISummary = a.CVScreeningResult != null ? a.CVScreeningResult.Summary : null
            })
            .ToListAsync(cancellationToken);

        return new GetApplicationsByJobResponse
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
