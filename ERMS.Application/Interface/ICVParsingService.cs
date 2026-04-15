namespace ERMS.Application.Interface;

public interface ICVParsingService
{
    Task<CVParsedInfoDto> ParseCVAsync(string resumeText, CancellationToken cancellationToken = default);
}

public sealed class CVParsedInfoDto
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? CurrentPosition { get; set; }
    public List<string> Skills { get; set; } = [];
}
