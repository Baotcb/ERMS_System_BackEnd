using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.Applications.Commands.ForwardApplication;

/// <summary>
/// Handler for forwarding an application from Applied to Shortlisted stage
/// Only HR Manager can perform this action
/// </summary>
public sealed class ForwardApplicationHandler : IRequestHandler<ForwardApplicationCommand, ForwardApplicationResult>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ForwardApplicationHandler> _logger;

    public ForwardApplicationHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        ILogger<ForwardApplicationHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ForwardApplicationResult> Handle(ForwardApplicationCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate current user is authenticated
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        // 2. Role check: HRManager ONLY (strict authorization)
        var userRoles = _currentUserService.Roles;
        if (userRoles == null || !userRoles.Contains(AppRoles.HRManager))
        {
            throw new UnauthorizedAccessException("Only HR Manager can forward applications.");
        }

        // 3. Enterprise scoping
        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync()
            ?? throw new UnauthorizedAccessException("User is not associated with any enterprise.");

        // 4. Load the application with related JobPosting
        var application = await _context.Applications
            .Include(a => a.JobPosting)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId && !a.IsDeleted, cancellationToken)
            ?? throw new Exception($"Application with ID {request.ApplicationId} not found.");

        // 5. Validate enterprise ownership
        if (application.JobPosting.EnterpriseId != enterpriseId)
        {
            throw new UnauthorizedAccessException("You do not have permission to access this application.");
        }

        // 6. Validate current stage is "Applied"
        if (!application.Stage.Equals(ApplicationStage.Applied, StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception($"Cannot forward application. Current stage is '{application.Stage}', expected '{ApplicationStage.Applied}'.");
        }

        // 7. Store previous stage for response
        var previousStage = application.Stage;

        // 8. Update application stage to Shortlisted
        application.Stage = ApplicationStage.Shortlisted;
        application.StageUpdatedAt = DateTime.UtcNow;
        application.UpdatedAt = DateTime.UtcNow;

        // 9. Update HR Note if provided
        if (!string.IsNullOrWhiteSpace(request.HRNote))
        {
            application.HRNote = request.HRNote.Trim();
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Application {ApplicationId} forwarded from {PreviousStage} to {NewStage} by user {UserId}",
            application.Id, previousStage, application.Stage, userId);

        return new ForwardApplicationResult
        {
            ApplicationId = application.Id,
            PreviousStage = previousStage,
            NewStage = application.Stage,
            StageUpdatedAt = application.StageUpdatedAt ?? DateTime.UtcNow,
            HRNote = application.HRNote
        };
    }
}
