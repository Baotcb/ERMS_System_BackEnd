using ERMS.Application.Interface;
using ERMS.Domain.Constants.Enterprise;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Recruitment;
using ERMS.Domain.Entities.System;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Admin.Commands.SetEnterpriseStatus;

public sealed class SetEnterpriseStatusHandler : IRequestHandler<SetEnterpriseStatusCommand, bool>
{
    private const string EnterpriseEntityType = "Enterprise";
    private static readonly HashSet<string> AllowedStatuses =
    [
        EnterpriseStatus.Active,
        EnterpriseStatus.Suspended,
        EnterpriseStatus.Locked,
        EnterpriseStatus.Inactive
    ];

    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public SetEnterpriseStatusHandler(IERMSDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(SetEnterpriseStatusCommand request, CancellationToken cancellationToken)
    {
        if (request.EnterpriseId == Guid.Empty)
        {
            throw new ArgumentException("EnterpriseId không hợp lệ.", nameof(request.EnterpriseId));
        }

        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            throw new UnauthorizedAccessException("Không tìm thấy thông tin người dùng.");
        }

        var userRoles = _currentUserService.Roles;
        if (userRoles == null || !userRoles.Contains(AppRoles.Admin))
        {
            throw new UnauthorizedAccessException("Chỉ Admin mới có quyền cập nhật trạng thái doanh nghiệp.");
        }

        if (!AllowedStatuses.Contains(request.NewStatus))
        {
            throw new ArgumentException("Trạng thái doanh nghiệp không hợp lệ.");
        }

        var enterprise = await _context.Enterprises
            .FirstOrDefaultAsync(
                item => item.Id == request.EnterpriseId && !item.IsDeleted,
                cancellationToken);

        if (enterprise == null)
        {
            throw new KeyNotFoundException("Không tìm thấy doanh nghiệp.");
        }

        if (enterprise.Status == request.NewStatus)
        {
            return true;
        }

        var reasonCategory = request.ReasonCategory?.Trim();
        var adminNote = request.AdminNote?.Trim();
        var historyNote = BuildHistoryNote(reasonCategory, adminNote);
        var previousStatus = enterprise.Status;

        enterprise.Status = request.NewStatus;

        _context.ApprovalHistories.Add(new ApprovalHistory
        {
            EntityType = EnterpriseEntityType,
            EntityId = enterprise.Id,
            Action = "StatusChange",
            PreviousStatus = previousStatus,
            NewStatus = request.NewStatus,
            PerformedById = userId.Value,
            Note = historyNote
        });

        if (request.SendNotification)
        {
            await AddNotificationsAsync(enterprise, request.NewStatus, reasonCategory, adminNote, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task AddNotificationsAsync(
        ERMS.Domain.Entities.Enterprise.Enterprise enterprise,
        string nextStatus,
        string? reasonCategory,
        string? adminNote,
        CancellationToken cancellationToken)
    {
        var recipientUserIds = await _context.Employees
            .AsNoTracking()
            .Where(employee => employee.EnterpriseId == enterprise.Id && !employee.IsDeleted)
            .Select(employee => employee.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (enterprise.CreatedById.HasValue && !recipientUserIds.Contains(enterprise.CreatedById.Value))
        {
            recipientUserIds.Add(enterprise.CreatedById.Value);
        }

        if (recipientUserIds.Count == 0)
        {
            return;
        }

        var title = $"Trạng thái doanh nghiệp: {nextStatus}";
        var message = BuildNotificationMessage(enterprise.EnterpriseName, nextStatus, reasonCategory, adminNote);

        var notifications = recipientUserIds.Select(recipientUserId => new Notification
        {
            Id = Guid.CreateVersion7(),
            UserId = recipientUserId,
            Title = title,
            Message = message,
            NotificationType = "EnterpriseStatusChange",
            EntityType = EnterpriseEntityType,
            EntityId = enterprise.Id,
            IsRead = false,
            IsSent = false
        });

        _context.Notifications.AddRange(notifications);
    }

    private static string? BuildHistoryNote(string? reasonCategory, string? adminNote)
    {
        if (string.IsNullOrWhiteSpace(reasonCategory))
        {
            return adminNote;
        }

        return string.IsNullOrWhiteSpace(adminNote)
            ? reasonCategory
            : $"{reasonCategory}: {adminNote}";
    }

    private static string BuildNotificationMessage(
        string enterpriseName,
        string nextStatus,
        string? reasonCategory,
        string? adminNote)
    {
        var message = $"Doanh nghiệp {enterpriseName} đã được cập nhật sang trạng thái {nextStatus}.";

        if (!string.IsNullOrWhiteSpace(reasonCategory))
        {
            message += $" Lý do: {reasonCategory}.";
        }

        if (!string.IsNullOrWhiteSpace(adminNote))
        {
            message += $" Ghi chú: {adminNote}.";
        }

        return message;
    }
}
