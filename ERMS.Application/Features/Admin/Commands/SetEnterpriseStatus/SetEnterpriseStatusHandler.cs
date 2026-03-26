using ERMS.Application.Interface;
using ERMS.Domain.Constants.Enterprise;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Recruitment;
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
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            throw new UnauthorizedAccessException("Khong tim thay thong tin nguoi dung.");
        }

        var userRoles = _currentUserService.Roles;
        if (userRoles == null || !userRoles.Contains(AppRoles.Admin))
        {
            throw new UnauthorizedAccessException("Chi Admin moi co quyen cap nhat trang thai doanh nghiep.");
        }

        if (!AllowedStatuses.Contains(request.NewStatus))
        {
            throw new ArgumentException("Trang thai doanh nghiep khong hop le.");
        }

        var enterprise = await _context.Enterprises
            .FirstOrDefaultAsync(
                item => item.Id == request.EnterpriseId && !item.IsDeleted,
                cancellationToken);

        if (enterprise == null)
        {
            throw new KeyNotFoundException("Khong tim thay doanh nghiep.");
        }

        if (enterprise.Status == request.NewStatus)
        {
            return true;
        }

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
            Note = request.AdminNote?.Trim()
        });

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
