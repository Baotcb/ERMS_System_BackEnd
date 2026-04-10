using MediatR;

namespace ERMS.Application.Features.Admin.Commands.SetEnterpriseStatus;

public sealed class SetEnterpriseStatusCommand : IRequest<bool>
{
    public Guid EnterpriseId { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public string? ReasonCategory { get; set; }
    public string? AdminNote { get; set; }
    public bool SendNotification { get; set; }
}
