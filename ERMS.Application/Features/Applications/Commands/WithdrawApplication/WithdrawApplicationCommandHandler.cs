using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.Applications.Commands.WithdrawApplication;

/// <summary>
/// Handler for withdrawing a candidate's own application.
/// Only the owning Candidate can perform this action.
/// </summary>
public sealed class WithdrawApplicationCommandHandler : IRequestHandler<WithdrawApplicationCommand, WithdrawApplicationResult>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<WithdrawApplicationCommandHandler> _logger;

    /// <summary>
    /// Terminal stages from which an application cannot be withdrawn.
    /// </summary>
    private static readonly string[] TerminalStages =
        [ApplicationStage.Rejected, ApplicationStage.Hired, ApplicationStage.Withdrawn];

    public WithdrawApplicationCommandHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        ILogger<WithdrawApplicationCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<WithdrawApplicationResult> Handle(WithdrawApplicationCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate current user is authenticated
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

        // 2. Role check: Candidate ONLY
        var userRoles = _currentUserService.Roles;
        if (userRoles == null || !userRoles.Contains(AppRoles.Candidate))
        {
            throw new UnauthorizedAccessException("Chỉ ứng viên mới có quyền rút hồ sơ ứng tuyển.");
        }

        // 3. Resolve the Candidate profile from the current user
        var candidate = await _context.Candidates
            .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted, cancellationToken)
            ?? throw new Exception("Không tìm thấy hồ sơ ứng viên.");

        // 4. Load the application
        var application = await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId && !a.IsDeleted, cancellationToken)
            ?? throw new Exception($"Không tìm thấy hồ sơ ứng tuyển với ID {request.ApplicationId}.");

        // 5. Validate ownership — the candidate can only withdraw their own application
        if (application.CandidateId != candidate.Id)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền rút hồ sơ này.");
        }

        // 6. Validate the application is not in a terminal stage
        if (Array.Exists(TerminalStages, s => s.Equals(application.Stage, StringComparison.OrdinalIgnoreCase)))
        {
            throw new Exception($"Không thể rút hồ sơ ứng tuyển. Giai đoạn hiện tại là '{application.Stage}', đã ở trạng thái kết thúc.");
        }

        // 7. Store previous stage for response
        var previousStage = application.Stage;

        // 8. Update the application
        application.Stage = ApplicationStage.Withdrawn;
        application.StageUpdatedAt = DateTime.UtcNow;
        application.Status = "Withdrawn";
        application.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Application {ApplicationId} withdrawn by candidate {CandidateId} (UserId: {UserId}). Previous stage: {PreviousStage}",
            application.Id, candidate.Id, userId, previousStage);

        return new WithdrawApplicationResult
        {
            ApplicationId = application.Id,
            PreviousStage = previousStage,
            NewStage = ApplicationStage.Withdrawn,
            WithdrawnAt = application.StageUpdatedAt ?? DateTime.UtcNow
        };
    }
}
