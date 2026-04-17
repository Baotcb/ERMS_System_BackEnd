namespace ERMS.Application.Features.Applications.Commands.ExtractCVInfo;

public sealed class ExtractCVInfoResult
{
    public string ResumeUrl { get; set; } = string.Empty;
    public string ResumePublicId { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string ResumeText { get; set; } = string.Empty;
}
