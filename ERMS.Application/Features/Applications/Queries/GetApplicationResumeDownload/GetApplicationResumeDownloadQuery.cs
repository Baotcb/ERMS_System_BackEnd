using MediatR;

namespace ERMS.Application.Features.Applications.Queries.GetApplicationResumeDownload;

public sealed record GetApplicationResumeDownloadQuery : IRequest<GetApplicationResumeDownloadResult>
{
    public Guid ApplicationId { get; init; }
}
