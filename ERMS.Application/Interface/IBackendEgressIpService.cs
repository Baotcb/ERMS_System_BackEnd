using ERMS.Application.Features.Diagnostics.Queries.GetBackendEgressIp;

namespace ERMS.Application.Interface;

public interface IBackendEgressIpService
{
    Task<GetBackendEgressIpResponse> GetPublicEgressIpAsync(CancellationToken cancellationToken);
}
