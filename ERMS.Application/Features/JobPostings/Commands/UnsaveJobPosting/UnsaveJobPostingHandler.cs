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
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        // 2. Role check: Candidate ONLY
        var userRoles = _currentUserService.Roles;
        if (userRoles == null || !userRoles.Contains(AppRoles.Candidate))
        {
            throw new UnauthorizedAccessException("Only candidates can unsave job postings.");
        }

        // 3. Resolve the Candidate profile from the current user
        var candidate = await _context.Candidates
            .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted, cancellationToken)
            ?? throw new Exception("Candidate profile not found.");

        // 4. Find the saved job record
        var savedJob = await _context.SavedJobs
            .FirstOrDefaultAsync(s => s.JobPostingId == request.JobPostingId
                                   && s.CandidateId == candidate.Id, cancellationToken);

        if (savedJob == null)
        {
            throw new Exception("Saved job not found.");
        }

        // 5. Ownership check (defense in depth — query already filters by CandidateId)
        if (savedJob.CandidateId != candidate.Id)
        {
            throw new UnauthorizedAccessException("You do not have permission to unsave this post.");
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
            Message = "Job posting unsaved successfully."
        };
    }
}
