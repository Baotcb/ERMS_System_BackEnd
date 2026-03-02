namespace ERMS.Application.Features.Interviews.Queries.GetMyInterviews;

/// <summary>
/// Paginated response containing the current user's interviews
/// </summary>
public sealed class GetMyInterviewsResponse
{
    public List<MyInterviewDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

/// <summary>
/// DTO representing a single interview from the interviewer's perspective
/// </summary>
public sealed class MyInterviewDto
{
    public Guid InterviewId { get; set; }
    public Guid ApplicationId { get; set; }
    public string CandidateName { get; set; } = null!;
    public string? CandidateEmail { get; set; }
    public string JobTitle { get; set; } = null!;
    public string InterviewType { get; set; } = null!;
    public string InterviewFormat { get; set; } = null!;
    public int RoundNumber { get; set; }
    public DateTime ScheduledAt { get; set; }
    public int Duration { get; set; }
    public string? Location { get; set; }
    public string? MeetingLink { get; set; }
    public string Status { get; set; } = null!;
    public string MyRole { get; set; } = null!;
    public string MyConfirmationStatus { get; set; } = null!;
    public bool HasSubmittedFeedback { get; set; }
}
