using MediatR;

namespace ERMS.Application.Features.Applications.Commands.RejectApplication;

/// <summary>
/// Command to reject an application from the HR side.
/// </summary>
public sealed class RejectApplicationCommand : IRequest<RejectApplicationResult>
{
    public Guid ApplicationId { get; set; }
    public string RejectionReason { get; set; } = null!;
}
