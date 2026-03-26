using ERMS.Application.Interface;
using ERMS.Domain.Constants.Enterprise;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Recruitment;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Admin.Commands.SetEnterpriseLockState;

public sealed class SetEnterpriseLockStateHandler : IRequestHandler<SetEnterpriseLockStateCommand, bool>
{
    private const string EnterpriseEntityType = "Enterprise";
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public SetEnterpriseLockStateHandler(IERMSDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(SetEnterpriseLockStateCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            throw new UnauthorizedAccessException("Không tìm thấy thông tin người dùng.");
        }

        var userRoles = _currentUserService.Roles;
        if (userRoles == null || !userRoles.Contains(AppRoles.Admin))
        {
            throw new UnauthorizedAccessException("Chỉ Admin mới có quyền khóa hoặc mở khóa doanh nghiệp.");
        }

        var enterprise = await _context.Enterprises
            .FirstOrDefaultAsync(
                item => item.Id == request.EnterpriseId && !item.IsDeleted,
                cancellationToken);

        if (enterprise == null)
        {
            throw new KeyNotFoundException("Không tìm thấy doanh nghiệp.");
        }

        var previousStatus = enterprise.Status;
        var newStatus = request.IsLocked
            ? EnterpriseStatus.Locked
            : EnterpriseStatus.Active;
        var action = request.IsLocked ? "Lock" : "Unlock";

        enterprise.Status = newStatus;

        _context.ApprovalHistories.Add(new ApprovalHistory
        {
            EntityType = EnterpriseEntityType,
            EntityId = enterprise.Id,
            Action = action,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            PerformedById = userId.Value
        });

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
