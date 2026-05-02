namespace ERMS.Application.Features.Applications.Queries.GetApplicationResumeDownload;

public sealed record GetApplicationResumeDownloadResult
{
    public string DownloadUrl { get; init; } = string.Empty;
}
