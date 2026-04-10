namespace ERMS.Application.Interface;

/// <summary>
/// Builds and sends candidate-facing rejection emails.
/// </summary>
public interface IRejectionEmailService
{
    Task SendRejectionEmailAsync(RejectionEmailContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Context used to compose a rejection email for a candidate.
/// </summary>
public sealed record RejectionEmailContext
{
    public required string CandidateEmail { get; init; }
    public required string CandidateName { get; init; }
    public required string JobTitle { get; init; }
    public required string RejectionReason { get; init; }
    public string[]? SkillGaps { get; init; }
    public string[]? Concerns { get; init; }
}
