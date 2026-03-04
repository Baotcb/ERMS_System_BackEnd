using MediatR;

namespace ERMS.Application.Features.Applications.Commands.SubmitFinalDecision;

/// <summary>
/// Command for Department Head to submit the final decision on an interview.
/// Triggers workflow: Fail → Rejected, Passed → OfferProcessing, NextRound → new Interview.
/// </summary>
public sealed record SubmitFinalDecisionCommand : IRequest<SubmitFinalDecisionResult>
{
    public Guid ApplicationId { get; init; }
    public Guid InterviewId { get; init; }
    public string Decision { get; init; } = null!;
    public int? OverallRating { get; init; }
    public string? OverallFeedback { get; init; }
    public string? Note { get; init; }
    
    /// <summary>
    /// Optional: If Decision is NextRound, provided Employee Ids will be assigned to Round 2.
    /// If null or empty, the interviewers from the current round are copied.
    /// </summary>
    public List<Guid>? NextRoundInterviewerIds { get; init; }
}
