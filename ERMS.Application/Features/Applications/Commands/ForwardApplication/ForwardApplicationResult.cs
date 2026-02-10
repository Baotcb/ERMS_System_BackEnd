namespace ERMS.Application.Features.Applications.Commands.ForwardApplication;

/// <summary>
/// Result of forwarding an application
/// </summary>
public sealed class ForwardApplicationResult
{
    public Guid ApplicationId { get; set; }
    public string PreviousStage { get; set; } = null!;
    public string NewStage { get; set; } = null!;
    public DateTime StageUpdatedAt { get; set; }
    public string? HRNote { get; set; }
}
