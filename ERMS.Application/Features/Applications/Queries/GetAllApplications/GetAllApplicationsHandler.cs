using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Applications.Queries.GetAllApplications;

/// <summary>
/// Handler for retrieving all applications across the enterprise for HR Manager
/// </summary>
public sealed class GetAllApplicationsHandler : IRequestHandler<GetAllApplicationsQuery, GetAllApplicationsResponse>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetAllApplicationsHandler(IERMSDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<GetAllApplicationsResponse> Handle(GetAllApplicationsQuery request, CancellationToken cancellationToken)
    {
        // 1. Validate current user is authenticated
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        // 2. Role check: HRManager only
        var userRoles = _currentUserService.Roles;
        if (userRoles == null || !userRoles.Contains(AppRoles.HRManager))
        {
            throw new UnauthorizedAccessException("Only HR Manager can view all enterprise applications.");
        }

        // 3. Enterprise scoping
        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync()
            ?? throw new UnauthorizedAccessException("User is not associated with any enterprise.");

        // 4. Build query: Applications whose JobPosting belongs to the enterprise
        var query = _context.Applications
            .Include(a => a.Candidate)
                .ThenInclude(c => c.User)
            .Include(a => a.JobPosting)
            .Include(a => a.Resume)
            .Include(a => a.CVScreeningResult)
            .Where(a => a.JobPosting.EnterpriseId == enterpriseId
                     && !a.JobPosting.IsDeleted
                     && !a.IsDeleted);

        // 5. Optional stage filter
        if (!string.IsNullOrWhiteSpace(request.StageFilter))
        {
            query = query.Where(a => a.Stage == request.StageFilter);
        }

        // 6. Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // 7. Sort by AppliedAt descending (newest first), then paginate
        var items = await query
            .OrderByDescending(a => a.AppliedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new EnterpriseApplicationDto
            {
                ApplicationId = a.Id,
                Stage = a.Stage,
                Status = a.Status,
                AppliedAt = a.AppliedAt,
                CandidateId = a.CandidateId,
                CandidateName = a.Candidate.User.FullName,
                CandidateEmail = a.Candidate.User.Email,
                CandidatePhone = a.Candidate.User.PhoneNumber,
                JobPostingId = a.JobPostingId,
                JobTitle = a.JobPosting.JobTitle,
                ResumeUrl = a.Resume != null ? a.Resume.FileUrl : null,
                OverallScore = a.CVScreeningResult != null ? a.CVScreeningResult.OverallScore : null
            })
            .ToListAsync(cancellationToken);

        return new GetAllApplicationsResponse
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
