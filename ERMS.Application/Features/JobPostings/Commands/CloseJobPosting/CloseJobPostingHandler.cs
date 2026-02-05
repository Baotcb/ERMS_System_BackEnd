using ERMS.Application.Interface;
using ERMS.Domain.Constants.Recruitment;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.JobPostings.Commands.CloseJobPosting;

public sealed class CloseJobPostingHandler : IRequestHandler<CloseJobPostingCommand, Unit>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CloseJobPostingHandler> _logger;

    public CloseJobPostingHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        ILogger<CloseJobPostingHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Unit> Handle(CloseJobPostingCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        var userRoles = _currentUserService.Roles;
        if (userRoles == null || !userRoles.Contains(AppRoles.HRManager))
        {
            throw new UnauthorizedAccessException("Only HR Manager can close job postings.");
        }

        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync()
            ?? throw new UnauthorizedAccessException("User is not associated with any enterprise.");

        var jobPosting = await _context.JobPostings
            .FirstOrDefaultAsync(jp =>
                jp.Id == request.Id
                && jp.EnterpriseId == enterpriseId
                && !jp.IsDeleted,
                cancellationToken)
            ?? throw new Exception($"JobPosting with ID {request.Id} not found.");

        if (!JobPostingStatus.CanClose(jobPosting.Status))
        {
            throw new Exception($"Cannot close. Current status '{jobPosting.Status}' must be 'Published'.");
        }

        jobPosting.Status = JobPostingStatus.Closed;
        jobPosting.ClosedAt = DateTime.UtcNow;
        jobPosting.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Closed JobPosting {JobPostingId}", request.Id);
        return Unit.Value;
    }
}
