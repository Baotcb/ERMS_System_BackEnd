namespace ERMS.Application.Features.Applications.Commands.SubmitApplication;

/// <summary>
/// Result of submitting a job application
/// </summary>
public sealed class SubmitApplicationResult
{
    public Guid ApplicationId { get; set; }
    public Guid ResumeId { get; set; }
    public string ResumeUrl { get; set; } = string.Empty;
    public string Stage { get; set; } = "Applied";
    public DateTime AppliedAt { get; set; }

    /// <summary>
    /// AI-generated CV screening results
    /// </summary>
    public CVScreeningResultSummary? CVScreeningResult { get; set; }
}

/// <summary>
/// Summary of CV screening from AI analysis
/// </summary>
public sealed class CVScreeningResultSummary
{
    public decimal OverallScore { get; set; }
    public decimal SkillMatchScore { get; set; }
    public decimal ExperienceMatchScore { get; set; }
    public decimal EducationMatchScore { get; set; }
    public List<string> MatchedSkills { get; set; } = [];
    public List<string> MissingSkills { get; set; } = [];
    public List<string> Strengths { get; set; } = [];
    public string Summary { get; set; } = string.Empty;
}
