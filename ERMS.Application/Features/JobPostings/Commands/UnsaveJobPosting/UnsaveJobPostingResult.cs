namespace ERMS.Application.Features.JobPostings.Commands.UnsaveJobPosting;

public sealed class UnsaveJobPostingResult
{
    public Guid JobPostingId { get; set; }
    public string Message { get; set; } = null!;
}
