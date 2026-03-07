using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.Applications.Queries.GetMyApplications;

/// <summary>
/// Handler for retrieving a candidate's own application history.
/// Only the authenticated Candidate can view their applications.
/// </summary>
public sealed class GetMyApplicationsHandler : IRequestHandler<GetMyApplicationsQuery, GetMyApplicationsResponse>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetMyApplicationsHandler> _logger;

    public GetMyApplicationsHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        ILogger<GetMyApplicationsHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<GetMyApplicationsResponse> Handle(GetMyApplicationsQuery request, CancellationToken cancellationToken)
    {
        // 1. Validate current user is authenticated
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

        // 2. Role check: Candidate ONLY
        var userRoles = _currentUserService.Roles;
        if (userRoles == null || !userRoles.Contains(AppRoles.Candidate))
        {
            throw new UnauthorizedAccessException("Chỉ ứng viên mới có quyền xem hồ sơ ứng tuyển của mình.");
        }

        // 3. Resolve the Candidate profile from the current user
        var candidate = await _context.Candidates
            .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted, cancellationToken)
            ?? throw new Exception("Không tìm thấy hồ sơ ứng viên.");

        // 4. Build query for the candidate's applications
        var query = _context.Applications
            .Include(a => a.JobPosting)
                .ThenInclude(j => j.Enterprise)
            .Include(a => a.Interviews)
            .Include(a => a.Offer)
            .Where(a => a.CandidateId == candidate.Id
                     && !a.IsDeleted);

        // 5. Apply optional stage filter
        if (!string.IsNullOrWhiteSpace(request.StageFilter))
        {
            query = query.Where(a => a.Stage == request.StageFilter);
        }

        // 6. Order by most recent first
        var orderedQuery = query.OrderByDescending(a => a.AppliedAt);

        // 7. Get total count for pagination
        var totalCount = await orderedQuery.CountAsync(cancellationToken);

        // 8. Paginate and project to DTO
        var items = await orderedQuery
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new CandidateApplicationDto
            {
                ApplicationId = a.Id,
                JobPostingId = a.JobPostingId,
                JobTitle = a.JobPosting.JobTitle,
                JobCode = a.JobPosting.JobCode,
                CompanyName = a.JobPosting.Enterprise.EnterpriseName,
                Location = a.JobPosting.Location,
                EmploymentType = a.JobPosting.EmploymentType,
                Stage = a.Stage,
                Status = a.Status,
                AppliedAt = a.AppliedAt,
                StageUpdatedAt = a.StageUpdatedAt,
                HasInterview = a.Interviews.Any(),
                HasOffer = a.Offer != null
            })
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Candidate {CandidateId} (UserId: {UserId}) retrieved {Count} applications (Page {Page})",
            candidate.Id, userId, items.Count, request.PageNumber);

        return new GetMyApplicationsResponse
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
