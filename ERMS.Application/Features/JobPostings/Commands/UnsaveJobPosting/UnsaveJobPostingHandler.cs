using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.JobPostings.Commands.UnsaveJobPosting;

/// <summary>
/// Handler for removing a saved job posting.
/// Returns 403 if the candidate tries to unsave a post they don't own.
/// </summary>
public sealed class UnsaveJobPostingHandler : IRequestHandler<UnsaveJobPostingCommand, UnsaveJobPostingResult>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UnsaveJobPostingHandler> _logger;

    public UnsaveJobPostingHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        ILogger<UnsaveJobPostingHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<UnsaveJobPostingResult> Handle(UnsaveJobPostingCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate current user is authenticated
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

        // 2. Role check: Candidate ONLY
        var userRoles = _currentUserService.Roles;
        if (userRoles == null || !userRoles.Contains(AppRoles.Candidate))
        {
            throw new UnauthorizedAccessException("Chỉ ứng viên mới có quyền bỏ lưu tin tuyển dụng.");
        }

        // 3. Resolve the Candidate profile from the current user
        var candidate = await _context.Candidates
            .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted, cancellationToken)
            ?? throw new Exception("Không tìm thấy hồ sơ ứng viên.");

        // 4. Find the saved job record
        var savedJob = await _context.SavedJobs
            .FirstOrDefaultAsync(s => s.JobPostingId == request.JobPostingId
                                   && s.CandidateId == candidate.Id, cancellationToken);

        if (savedJob == null)
        {
            throw new Exception("Không tìm thấy bài đăng đã lưu.");
        }

        // 5. Ownership check (defense in depth — query already filters by CandidateId)
        if (savedJob.CandidateId != candidate.Id)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền bỏ lưu bài đăng này.");
        }

        // 6. Remove and save
        _context.SavedJobs.Remove(savedJob);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Candidate {CandidateId} (UserId: {UserId}) unsaved job posting {JobPostingId}",
            candidate.Id, userId, request.JobPostingId);

        return new UnsaveJobPostingResult
        {
            JobPostingId = request.JobPostingId,
            Message = "Đã bỏ lưu tin tuyển dụng thành công."
        };
    }
}
