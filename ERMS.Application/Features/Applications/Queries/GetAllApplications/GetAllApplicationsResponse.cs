namespace ERMS.Application.Features.Applications.Queries.GetAllApplications;

/// <summary>
/// Response containing paginated list of all applications across the enterprise
/// </summary>
public sealed class GetAllApplicationsResponse
{
    public List<EnterpriseApplicationDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

/// <summary>
/// Lean DTO for enterprise-wide application list view
/// </summary>
public sealed class EnterpriseApplicationDto
{
    // Application info
    public Guid ApplicationId { get; set; }
    public string Stage { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime AppliedAt { get; set; }

    // Candidate info
    public Guid CandidateId { get; set; }
    public string CandidateName { get; set; } = null!;
    public string? CandidateEmail { get; set; }
    public string? CandidatePhone { get; set; }
    public bool IsExternal { get; set; }
    public string? Source { get; set; }

    // Job info
    public Guid JobPostingId { get; set; }
    public string JobTitle { get; set; } = null!;

    // CV
    public string? ResumeUrl { get; set; }

    // AI Score summary
    public decimal? OverallScore { get; set; }
}
