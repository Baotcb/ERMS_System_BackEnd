using ERMS.Application.Interface;
using ERMS.Domain.Constants.Enterprise;
using ERMS.Domain.Constants.Recruitment;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Constants.System;
using ERMS.Domain.Entities.System;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Reports.Commands.ProcessReport;

public sealed class ProcessReportHandler : IRequestHandler<ProcessReportCommand, Unit>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ProcessReportHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Unit> Handle(ProcessReportCommand request, CancellationToken cancellationToken)
    {
        var adminId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var roles = _currentUserService.Roles;
        if (roles == null || !roles.Contains(AppRoles.Admin))
        {
            throw new UnauthorizedAccessException("Only admins can process reports.");
        }

        await using var transaction = await _context.BeginTransactionAsync(cancellationToken);
        try
        {
            var report = await _context.Reports
                .FirstOrDefaultAsync(x => x.Id == request.ReportId, cancellationToken)
                ?? throw new KeyNotFoundException("Report was not found.");

            if (report.Status != ReportConstants.Status.Pending && report.Status != ReportConstants.Status.Reviewing)
            {
                throw new InvalidOperationException("Report is already processed.");
            }

            var now = DateTime.UtcNow;
            var action = request.Action.Trim();
            var adminNote = string.IsNullOrWhiteSpace(request.AdminNote) ? null : request.AdminNote.Trim();

            switch (action)
            {
                case "Dismiss":
                    ApplyResolution(report, ReportConstants.Status.Dismissed, ReportConstants.ActionTaken.Dismissed, adminId, now, adminNote);
                    break;

                case "Resolve":
                    ApplyResolution(report, ReportConstants.Status.Resolved, ReportConstants.ActionTaken.Resolved, adminId, now, adminNote);
                    break;

                case "HideJobPosting":
                    await HandleHideJobPostingAsync(report, adminId, now, adminNote, cancellationToken);
                    break;

                case "SuspendEnterprise":
                    await HandleSuspendEnterpriseAsync(report, adminId, now, adminNote, cancellationToken);
                    break;

                default:
                    throw new ArgumentException("Action is not valid.", nameof(request.Action));
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Unit.Value;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task HandleHideJobPostingAsync(
        Report report,
        Guid adminId,
        DateTime now,
        string? adminNote,
        CancellationToken cancellationToken)
    {
        if (report.EntityType != ReportConstants.EntityType.JobPosting)
        {
            throw new InvalidOperationException("HideJobPosting action can only be used for JobPosting reports.");
        }

        var jobPosting = await _context.JobPostings
            .FirstOrDefaultAsync(x => x.Id == report.EntityId && !x.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException("Job posting was not found.");

        jobPosting.Status = JobPostingStatus.Archived;
        jobPosting.UpdatedAt = now;

        await ResolveOpenReportsForEntityAsync(
            report.EntityType,
            report.EntityId,
            ReportConstants.ActionTaken.HidJobPosting,
            adminId,
            now,
            adminNote,
            cancellationToken);
    }

    private async Task HandleSuspendEnterpriseAsync(
        Report report,
        Guid adminId,
        DateTime now,
        string? adminNote,
        CancellationToken cancellationToken)
    {
        if (report.EntityType != ReportConstants.EntityType.Enterprise)
        {
            throw new InvalidOperationException("SuspendEnterprise action can only be used for Enterprise reports.");
        }

        var enterprise = await _context.Enterprises
            .FirstOrDefaultAsync(x => x.Id == report.EntityId && !x.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException("Enterprise was not found.");

        enterprise.Status = EnterpriseStatus.Suspended;
        enterprise.UpdatedAt = now;

        await ResolveOpenReportsForEntityAsync(
            report.EntityType,
            report.EntityId,
            ReportConstants.ActionTaken.SuspendedEnterprise,
            adminId,
            now,
            adminNote,
            cancellationToken);
    }

    private async Task ResolveOpenReportsForEntityAsync(
        string entityType,
        Guid entityId,
        string actionTaken,
        Guid adminId,
        DateTime resolvedAt,
        string? adminNote,
        CancellationToken cancellationToken)
    {
        var openReports = await _context.Reports
            .Where(x =>
                x.EntityType == entityType
                && x.EntityId == entityId
                && (x.Status == ReportConstants.Status.Pending || x.Status == ReportConstants.Status.Reviewing))
            .ToListAsync(cancellationToken);

        foreach (var openReport in openReports)
        {
            ApplyResolution(
                openReport,
                ReportConstants.Status.Resolved,
                actionTaken,
                adminId,
                resolvedAt,
                adminNote);
        }
    }

    private static void ApplyResolution(
        Report report,
        string nextStatus,
        string actionTaken,
        Guid adminId,
        DateTime resolvedAt,
        string? adminNote)
    {
        report.Status = nextStatus;
        report.ActionTaken = actionTaken;
        report.ResolvedById = adminId;
        report.ResolvedAt = resolvedAt;
        report.AdminNote = adminNote;
        report.UpdatedAt = resolvedAt;
    }
}
