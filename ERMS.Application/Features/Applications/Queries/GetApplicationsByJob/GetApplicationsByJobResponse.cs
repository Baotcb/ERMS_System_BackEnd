namespace ERMS.Application.Features.Applications.Queries.GetApplicationsByJob;

/// <summary>
/// Response containing paginated list of applications for a job posting
/// </summary>
public sealed class GetApplicationsByJobResponse
{
    public Guid JobPostingId { get; set; }
    public string JobTitle { get; set; } = null!;
    public List<ApplicationListDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

/// <summary>
/// DTO for application in the list view
/// </summary>
public sealed class ApplicationListDto
{
    public Guid ApplicationId { get; set; }
    public Guid? CandidateId { get; set; }
    public string CandidateName { get; set; } = null!;
    public string? CandidateEmail { get; set; }
    public string? CandidatePhone { get; set; }
    public string? ResumeUrl { get; set; }
    public string Stage { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime AppliedAt { get; set; }
    public string? HRNote { get; set; }
    public bool IsExternal { get; set; }
    public string? Source { get; set; }

    // CV Screening Result
    public decimal? OverallScore { get; set; }
    public decimal? SkillMatchScore { get; set; }
    public decimal? ExperienceMatchScore { get; set; }
    public decimal? EducationMatchScore { get; set; }
    public decimal? KeywordMatchScore { get; set; }
    public List<string>? MatchedSkills { get; set; }
    public List<string>? MissingSkills { get; set; }
    public List<string>? Strengths { get; set; }
    public List<string>? Concerns { get; set; }
    public string? AISummary { get; set; }
}
