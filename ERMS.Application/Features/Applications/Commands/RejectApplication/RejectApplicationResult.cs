namespace ERMS.Application.Features.Applications.Commands.RejectApplication;

/// <summary>
/// Result returned after successfully rejecting an application.
/// </summary>
public sealed class RejectApplicationResult
{
    public Guid ApplicationId { get; set; }
    public string PreviousStage { get; set; } = null!;
    public string NewStage { get; set; } = null!;
    public DateTime RejectedAt { get; set; }
}
