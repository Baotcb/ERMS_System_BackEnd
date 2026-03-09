using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.JobPostings.Queries.GetMySavedPosts;

/// <summary>
/// Handler for retrieving a candidate's saved job postings.
/// Only the authenticated Candidate can view their saved posts.
/// </summary>
public sealed class GetMySavedPostsHandler : IRequestHandler<GetMySavedPostsQuery, GetMySavedPostsResponse>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetMySavedPostsHandler> _logger;

    public GetMySavedPostsHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        ILogger<GetMySavedPostsHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<GetMySavedPostsResponse> Handle(GetMySavedPostsQuery request, CancellationToken cancellationToken)
    {
        // 1. Validate current user is authenticated
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

        // 2. Role check: Candidate ONLY
        var userRoles = _currentUserService.Roles;
        if (userRoles == null || !userRoles.Contains(AppRoles.Candidate))
        {
            throw new UnauthorizedAccessException("Chỉ ứng viên mới có quyền xem bài viết đã lưu.");
        }

        // 3. Resolve the Candidate profile from the current user
        var candidate = await _context.Candidates
            .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted, cancellationToken)
            ?? throw new Exception("Không tìm thấy hồ sơ ứng viên.");

        // 4. Build query for the candidate's saved jobs
        var query = _context.SavedJobs
            .Include(s => s.JobPosting)
                .ThenInclude(j => j.Enterprise)
            .Where(s => s.CandidateId == candidate.Id)
            .OrderByDescending(s => s.SavedAt);

        // 5. Get total count for pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // 6. Paginate and project to DTO
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new SavedPostDto
            {
                SavedJobId = s.Id,
                JobPostingId = s.JobPostingId,
                JobTitle = s.JobPosting.JobTitle,
                JobCode = s.JobPosting.JobCode,
                CompanyName = s.JobPosting.Enterprise.EnterpriseName,
                Location = s.JobPosting.Location,
                EmploymentType = s.JobPosting.EmploymentType,
                Status = s.JobPosting.Status,
                SavedAt = s.SavedAt
            })
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Candidate {CandidateId} (UserId: {UserId}) retrieved {Count} saved posts (Page {Page})",
            candidate.Id, userId, items.Count, request.PageNumber);

        return new GetMySavedPostsResponse
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
