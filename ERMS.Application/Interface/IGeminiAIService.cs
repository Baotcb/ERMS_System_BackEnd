namespace ERMS.Application.Interface;

/// <summary>
/// DTO for AI-generated Job Description result
/// </summary>
public class GenerateJDResultDto
{
    public string Description { get; set; } = string.Empty;
    public string? Requirements { get; set; }
    public string? Benefits { get; set; }
}

/// <summary>
/// DTO for CV screening results from Gemini AI
/// </summary>
public class CVScreeningResultDto
{
    public decimal OverallScore { get; set; }
    public decimal SkillMatchScore { get; set; }
    public decimal ExperienceMatchScore { get; set; }
    public decimal EducationMatchScore { get; set; }
    public decimal KeywordMatchScore { get; set; }
    public List<string> MatchedSkills { get; set; } = [];
    public List<string> MissingSkills { get; set; } = [];
    public List<string> Strengths { get; set; } = [];
    public List<string> Concerns { get; set; } = [];
    public string Summary { get; set; } = string.Empty;
    public string RawResponse { get; set; } = string.Empty;
}

/// <summary>
/// Interface for Gemini AI resume screening service
/// </summary>
public interface IGeminiAIService
{
    /// <summary>
    /// Analyzes a resume against job requirements using Gemini AI
    /// </summary>
    /// <param name="resumeText">Extracted text from the resume</param>
    /// <param name="jobDescription">Job description text</param>
    /// <param name="requiredSkills">Required skills (JSON array or comma-separated)</param>
    /// <param name="educationLevel">Required education level</param>
    /// <param name="experienceLevel">Required experience level</param>
    /// <returns>CV screening results with scores</returns>
    Task<CVScreeningResultDto> AnalyzeResumeAsync(
        string resumeText,
        string jobDescription,
        string requiredSkills,
        string? educationLevel,
        string? experienceLevel);

    /// <summary>
    /// Generates a professional job description in Vietnamese using Gemini AI
    /// </summary>
    Task<GenerateJDResultDto> GenerateJobDescriptionAsync(
        string positionTitle,
        string? justification,
        string? requiredSkills,
        int? minExperience,
        int? maxExperience,
        string? educationLevel,
        decimal? salaryRangeMin,
        decimal? salaryRangeMax);
}
