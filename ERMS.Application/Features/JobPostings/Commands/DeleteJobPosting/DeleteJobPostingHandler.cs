using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.JobPostings.Commands.DeleteJobPosting;

public sealed class DeleteJobPostingHandler : IRequestHandler<DeleteJobPostingCommand, Unit>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DeleteJobPostingHandler> _logger;

    public DeleteJobPostingHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        ILogger<DeleteJobPostingHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Unit> Handle(DeleteJobPostingCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        var userRoles = _currentUserService.Roles;
        if (userRoles == null || !userRoles.Contains(AppRoles.HRManager))
        {
            throw new UnauthorizedAccessException("Only HR Manager can delete job postings.");
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

        jobPosting.IsDeleted = true;
        jobPosting.DeletedAt = DateTime.UtcNow;
        jobPosting.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Soft-deleted JobPosting {JobPostingId}", request.Id);
        return Unit.Value;
    }
}
