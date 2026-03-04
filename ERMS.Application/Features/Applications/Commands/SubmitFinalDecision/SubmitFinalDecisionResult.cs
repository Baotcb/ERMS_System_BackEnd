namespace ERMS.Application.Features.Applications.Commands.SubmitFinalDecision;

/// <summary>
/// Result returned after Department Head submits the final decision.
/// </summary>
public sealed record SubmitFinalDecisionResult
{
    public Guid InterviewId { get; init; }
    public string Decision { get; init; } = null!;
    public string ApplicationStage { get; init; } = null!;
    public Guid? NewInterviewId { get; init; }
}
