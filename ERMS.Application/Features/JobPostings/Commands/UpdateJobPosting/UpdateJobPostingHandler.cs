using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.JobPostings.Commands.UpdateJobPosting;

public sealed class UpdateJobPostingHandler : IRequestHandler<UpdateJobPostingCommand, Unit>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateJobPostingHandler> _logger;

    public UpdateJobPostingHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        ILogger<UpdateJobPostingHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Unit> Handle(UpdateJobPostingCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        var userRoles = _currentUserService.Roles;
        if (userRoles == null || !userRoles.Contains(AppRoles.HRManager))
        {
            throw new UnauthorizedAccessException("Only HR Manager can update job postings.");
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

        // Update only allowed fields
        if (request.Description != null)
            jobPosting.Description = request.Description.Trim();

        if (request.Benefits != null)
            jobPosting.Benefits = request.Benefits.Trim();

        if (request.ApplicationDeadline.HasValue)
        {
            if (request.ApplicationDeadline.Value <= DateTime.UtcNow)
                throw new Exception("Application deadline must be in the future.");
            jobPosting.ApplicationDeadline = request.ApplicationDeadline.Value;
        }

        if (request.Location != null)
            jobPosting.Location = request.Location.Trim();

        if (request.RemoteOption != null)
            jobPosting.RemoteOption = request.RemoteOption.Trim();

        jobPosting.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated JobPosting {JobPostingId}", request.Id);
        return Unit.Value;
    }
}
