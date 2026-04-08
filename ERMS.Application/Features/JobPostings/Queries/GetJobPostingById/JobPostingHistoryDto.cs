namespace ERMS.Application.Features.JobPostings.Queries.GetJobPostingById;

public sealed class JobPostingHistoryDto
{
    public string Action { get; set; } = null!;
    public string? PreviousStatus { get; set; }
    public string NewStatus { get; set; } = null!;
    public string PerformedByName { get; set; } = null!;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}
