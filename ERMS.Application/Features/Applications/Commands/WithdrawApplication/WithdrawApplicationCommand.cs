using MediatR;

namespace ERMS.Application.Features.Applications.Commands.WithdrawApplication;

/// <summary>
/// Command for a candidate to withdraw their own application
/// </summary>
public sealed class WithdrawApplicationCommand : IRequest<WithdrawApplicationResult>
{
    /// <summary>
    /// The Application ID to withdraw
    /// </summary>
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Optional reason for withdrawing the application
    /// </summary>
    public string? Reason { get; set; }
}
