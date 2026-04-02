using System.Text.Json;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Application;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ApplicationEntity = ERMS.Domain.Entities.Application.Application;

namespace ERMS.Application.Features.Applications.Commands.RejectApplication;

/// <summary>
/// Handler for rejecting an application from the HR side.
/// </summary>
public sealed class RejectApplicationHandler : IRequestHandler<RejectApplicationCommand, RejectApplicationResult>
{
    private static readonly string[] AllowedStages =
        [ApplicationStage.Applied, ApplicationStage.Reviewing, ApplicationStage.Shortlisted];

    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IRejectionEmailService _rejectionEmailService;
    private readonly ILogger<RejectApplicationHandler> _logger;

    public RejectApplicationHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        IRejectionEmailService rejectionEmailService,
        ILogger<RejectApplicationHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _rejectionEmailService = rejectionEmailService;
        _logger = logger;
    }

    public async Task<RejectApplicationResult> Handle(RejectApplicationCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync()
            ?? throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào.");

        var userRoles = _currentUserService.Roles;
        if (userRoles == null || !userRoles.Contains(AppRoles.HRManager))
        {
            throw new UnauthorizedAccessException("Chỉ HR Manager mới có quyền từ chối hồ sơ ứng tuyển.");
        }

        var application = await _context.Applications
            .Include(a => a.JobPosting)
            .Include(a => a.Candidate)
                .ThenInclude(c => c.User)
            .Include(a => a.CVScreeningResult)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId && !a.IsDeleted, cancellationToken)
            ?? throw new Exception($"Không tìm thấy hồ sơ ứng tuyển với ID {request.ApplicationId}.");

        if (application.JobPosting.EnterpriseId != enterpriseId)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền truy cập hồ sơ ứng tuyển này.");
        }

        if (!Array.Exists(AllowedStages, stage => stage.Equals(application.Stage, StringComparison.OrdinalIgnoreCase)))
        {
            throw new Exception($"Không thể từ chối hồ sơ. Giai đoạn hiện tại là '{application.Stage}'.");
        }

        var rejectionReason = request.RejectionReason.Trim();
        var previousStage = application.Stage;
        var rejectedAt = DateTime.UtcNow;

        application.Stage = ApplicationStage.Rejected;
        application.RejectionReason = rejectionReason;
        application.RejectedById = userId;
        application.RejectedAt = rejectedAt;
        application.StageUpdatedAt = rejectedAt;
        application.UpdatedAt = rejectedAt;

        await _context.SaveChangesAsync(cancellationToken);

        await TrySendRejectionEmailAsync(application, rejectionReason, cancellationToken);

        _logger.LogInformation(
            "Application {ApplicationId} rejected from {PreviousStage} to {NewStage} by user {UserId}",
            application.Id,
            previousStage,
            application.Stage,
            userId);

        return new RejectApplicationResult
        {
            ApplicationId = application.Id,
            PreviousStage = previousStage,
            NewStage = application.Stage,
            RejectedAt = rejectedAt
        };
    }

    private async Task TrySendRejectionEmailAsync(ApplicationEntity application, string rejectionReason, CancellationToken cancellationToken)
    {
        var candidateUser = application.Candidate?.User;
        if (candidateUser == null || string.IsNullOrWhiteSpace(candidateUser.Email))
        {
            _logger.LogWarning(
                "Skipped rejection email for Application {ApplicationId} because candidate email is missing.",
                application.Id);
            return;
        }

        try
        {
            await _rejectionEmailService.SendRejectionEmailAsync(
                new RejectionEmailContext
                {
                    CandidateEmail = candidateUser.Email,
                    CandidateName = candidateUser.FullName,
                    JobTitle = application.JobPosting.JobTitle,
                    RejectionReason = rejectionReason,
                    SkillGaps = ParseJsonArray(application.CVScreeningResult?.MissingSkills),
                    Concerns = ParseJsonArray(application.CVScreeningResult?.Concerns)
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send rejection email for Application {ApplicationId}", application.Id);
        }
    }

    private static string[]? ParseJsonArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<string[]>(json);
        }
        catch
        {
            return null;
        }
    }
}
