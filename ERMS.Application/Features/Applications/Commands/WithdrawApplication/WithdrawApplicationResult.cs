namespace ERMS.Application.Features.Applications.Commands.WithdrawApplication;

/// <summary>
/// Result of withdrawing an application
/// </summary>
public sealed class WithdrawApplicationResult
{
    public Guid ApplicationId { get; set; }
    public string PreviousStage { get; set; } = null!;
    public string NewStage { get; set; } = null!;
    public DateTime WithdrawnAt { get; set; }
}
