namespace ERMS.Application.Features.Applications.Commands.ExtractCvInfo;

public sealed class ExtractCvInfoResult
{
    public string ResumeUrl { get; set; } = null!;
    public string ResumePublicId { get; set; } = null!;
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    /// <summary>
    /// Extracted PDF text — pass back to AddExternalApplication to avoid re-uploading
    /// </summary>
    public string ResumeText { get; set; } = null!;
}
