namespace ERMS.Application.Interface;

/// <summary>
/// Provider-agnostic AI service interface for resume analysis and JD generation.
/// DTOs (CVScreeningResultDto, GenerateJDResultDto) are defined in IGeminiAIService.cs.
/// </summary>
public interface IAIService
{
    Task<CVScreeningResultDto> AnalyzeResumeAsync(
        string resumeText,
        string jobDescription,
        string requiredSkills,
        string? educationLevel,
        string? experienceLevel);

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
