namespace ERMS.Application.Features.Applications.Commands.SubmitInterviewFeedback;

/// <summary>
/// Result returned after an interviewer submits their feedback.
/// </summary>
public sealed record SubmitInterviewFeedbackResult
{
    public Guid ParticipantId { get; init; }
    public Guid InterviewId { get; init; }
    public int Rating { get; init; }
    public DateTime FeedbackSubmittedAt { get; init; }
}
