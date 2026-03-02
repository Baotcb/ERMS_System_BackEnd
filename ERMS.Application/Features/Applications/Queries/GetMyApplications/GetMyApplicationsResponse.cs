namespace ERMS.Application.Features.Applications.Queries.GetMyApplications;

/// <summary>
/// Response containing the candidate's application history with pagination
/// </summary>
public sealed class GetMyApplicationsResponse
{
    public List<CandidateApplicationDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

public sealed class CandidateApplicationDto
{
    public Guid ApplicationId { get; set; }
    public Guid JobPostingId { get; set; }
    public string JobTitle { get; set; } = null!;
    public string? JobCode { get; set; }
    public string? CompanyName { get; set; }
    public string? Location { get; set; }
    public string EmploymentType { get; set; } = null!;
    public string Stage { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime AppliedAt { get; set; }
    public DateTime? StageUpdatedAt { get; set; }
    public bool HasInterview { get; set; }
    public bool HasOffer { get; set; }
}
