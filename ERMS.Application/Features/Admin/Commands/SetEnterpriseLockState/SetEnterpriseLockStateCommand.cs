using MediatR;

namespace ERMS.Application.Features.Admin.Commands.SetEnterpriseLockState;

public sealed class SetEnterpriseLockStateCommand : IRequest<bool>
{
    public Guid EnterpriseId { get; set; }
    public bool IsLocked { get; set; }
}
