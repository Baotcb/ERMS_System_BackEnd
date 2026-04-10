namespace ERMS.Application.Interface;

/// <summary>
/// DTO for extracted candidate contact info from a CV
/// </summary>
public class ExtractedCvInfoDto
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
}

/// <summary>
/// Extracts candidate contact information (name, email, phone) from CV text using AI
/// </summary>
public interface ICvInfoExtractorService
{
    Task<ExtractedCvInfoDto> ExtractContactInfoAsync(
        string resumeText,
        byte[]? pdfBytes = null,
        CancellationToken cancellationToken = default);
}
