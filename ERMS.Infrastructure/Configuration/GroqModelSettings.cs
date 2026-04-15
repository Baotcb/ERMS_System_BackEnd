namespace ERMS.Infrastructure.Configuration;

public sealed class GroqModelSettings
{
    public string CvParsing { get; set; } = string.Empty;
    public string CvScoring { get; set; } = string.Empty;
    public string JobDescription { get; set; } = string.Empty;
    public string Probe { get; set; } = string.Empty;
}
