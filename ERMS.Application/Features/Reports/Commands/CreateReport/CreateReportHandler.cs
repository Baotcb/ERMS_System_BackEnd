using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Constants.System;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.System;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Reports.Commands.CreateReport;

public sealed class CreateReportHandler : IRequestHandler<CreateReportCommand, Guid>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly UserManager<User> _userManager;

    public CreateReportHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        UserManager<User> userManager)
    {
        _context = context;
        _currentUserService = currentUserService;
        _userManager = userManager;
    }

    public async Task<Guid> Handle(CreateReportCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var roles = _currentUserService.Roles;
        if (roles == null || !roles.Contains(AppRoles.Candidate))
        {
            throw new UnauthorizedAccessException("Only candidates can submit reports.");
        }

        var entityType = request.EntityType.Trim();
        var reason = request.Reason.Trim();

        var exists = entityType switch
        {
            var t when t == ReportConstants.EntityType.JobPosting =>
                await _context.JobPostings.AnyAsync(x => x.Id == request.EntityId && !x.IsDeleted, cancellationToken),
            var t when t == ReportConstants.EntityType.Enterprise =>
                await _context.Enterprises.AnyAsync(x => x.Id == request.EntityId && !x.IsDeleted, cancellationToken),
            _ => throw new ArgumentException("Invalid entity type.", nameof(request.EntityType))
        };

        if (!exists)
        {
            throw new KeyNotFoundException("Reported entity was not found.");
        }

        var isDuplicateOpenReport = await _context.Reports.AnyAsync(
            x => x.ReportedById == userId
                && x.EntityType == entityType
                && x.EntityId == request.EntityId
                && (x.Status == ReportConstants.Status.Pending || x.Status == ReportConstants.Status.Reviewing),
            cancellationToken);

        if (isDuplicateOpenReport)
        {
            throw new InvalidOperationException("You already have a pending report for this entity.");
        }

        var report = new Report
        {
            Id = Guid.CreateVersion7(),
            ReportedById = userId,
            EntityType = entityType,
            EntityId = request.EntityId,
            Reason = reason,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Status = ReportConstants.Status.Pending
        };

        _context.Reports.Add(report);

        var admins = await _userManager.GetUsersInRoleAsync(AppRoles.Admin);
        if (admins.Count > 0)
        {
            var notifications = admins.Select(admin => new Notification
            {
                Id = Guid.CreateVersion7(),
                UserId = admin.Id,
                Title = "B\u00E1o c\u00E1o vi ph\u1EA1m m\u1EDBi",
                Message = "C\u00F3 b\u00E1o c\u00E1o m\u1EDBi t\u1EEB \u1EE9ng vi\u00EAn, vui l\u00F2ng ki\u1EC3m tra.",
                NotificationType = "ReportCreated",
                EntityType = "Report",
                EntityId = report.Id,
                ActionUrl = "/admin/reports",
                IsRead = false,
                IsSent = false
            });

            _context.Notifications.AddRange(notifications);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return report.Id;
    }
}

