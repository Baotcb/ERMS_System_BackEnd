using MediatR;

namespace ERMS.Application.Features.Applications.Commands.SubmitInterviewFeedback;

/// <summary>
/// Command for an interviewer to submit their individual feedback on an interview.
/// Updates only the InterviewParticipant record. Does NOT affect Interview status or Application stage.
/// </summary>
public sealed record SubmitInterviewFeedbackCommand : IRequest<SubmitInterviewFeedbackResult>
{
    public Guid ApplicationId { get; init; }
    public Guid InterviewId { get; init; }
    public int Rating { get; init; }
    public string Feedback { get; init; } = null!;
    public string? Recommendation { get; init; }
}
