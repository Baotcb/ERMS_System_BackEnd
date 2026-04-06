using ERMS.Application.Features.Admin.Queries.GetSystemIntegrations;

namespace ERMS.Application.Interface;

public interface ISystemIntegrationStatusService
{
    Task<GetSystemIntegrationsResponse> GetSystemIntegrationsAsync(CancellationToken cancellationToken);
}
