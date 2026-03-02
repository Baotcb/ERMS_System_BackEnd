namespace ERMS.Application.Features.Applications.Queries.GetShortlistedApplications;

/// <summary>
/// Response containing paginated shortlisted applications sorted by AI match score
/// </summary>
public sealed record GetShortlistedApplicationsResponse
{
    public Guid PlanDetailId { get; init; }
    public string PositionTitle { get; init; } = string.Empty;
    public IReadOnlyList<ShortlistedApplicationDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
}

/// <summary>
/// DTO for a shortlisted application with candidate and CV screening details
/// </summary>
public sealed record ShortlistedApplicationDto
{
    public Guid ApplicationId { get; init; }
    public Guid CandidateId { get; init; }
    public string CandidateName { get; init; } = string.Empty;
    public string? CandidateEmail { get; init; }
    public string? CandidatePhone { get; init; }
    public string? ResumeUrl { get; init; }
    public string Stage { get; init; } = string.Empty;
    public DateTime AppliedAt { get; init; }
    public string? HRNote { get; init; }

    // CV Screening Results
    public decimal? OverallScore { get; init; }
    public decimal? SkillMatchScore { get; init; }
    public decimal? ExperienceMatchScore { get; init; }
    public decimal? EducationMatchScore { get; init; }
    public string? AISummary { get; init; }
    public string? MatchedSkills { get; init; }
    public string? MissingSkills { get; init; }
}
