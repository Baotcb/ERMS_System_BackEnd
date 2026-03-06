using MediatR;

namespace ERMS.Application.Features.Applications.Commands.ConfirmHire;

/// <summary>
/// Command for HR to confirm hiring a candidate after contract signing.
/// ApplicationId is passed in the request body (not URL) to prevent IDOR.
/// </summary>
public sealed class ConfirmHireCommand : IRequest<ConfirmHireResult>
{
    /// <summary>
    /// The Application ID to confirm hire for
    /// </summary>
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// The new corporate email for the employee account.
    /// HR assigns a dedicated corporate email to the new hire.
    /// </summary>
    public string EmployeeEmail { get; set; } = null!;
}
