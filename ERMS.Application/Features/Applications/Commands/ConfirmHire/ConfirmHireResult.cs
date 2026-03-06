namespace ERMS.Application.Features.Applications.Commands.ConfirmHire;

/// <summary>
/// Result returned after successfully confirming a hire
/// </summary>
public sealed class ConfirmHireResult
{
    public Guid ApplicationId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = null!;
    public string NewStage { get; set; } = null!;
    public string EmployeeEmail { get; set; } = null!;
}
