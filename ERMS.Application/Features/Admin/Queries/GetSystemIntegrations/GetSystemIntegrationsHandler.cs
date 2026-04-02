using ERMS.Application.Interface;
using MediatR;

namespace ERMS.Application.Features.Admin.Queries.GetSystemIntegrations;

public sealed class GetSystemIntegrationsHandler : IRequestHandler<GetSystemIntegrationsQuery, GetSystemIntegrationsResponse>
{
    private readonly ISystemIntegrationStatusService _integrationStatusService;

    public GetSystemIntegrationsHandler(ISystemIntegrationStatusService integrationStatusService)
    {
        _integrationStatusService = integrationStatusService;
    }

    public Task<GetSystemIntegrationsResponse> Handle(GetSystemIntegrationsQuery request, CancellationToken cancellationToken)
    {
        return _integrationStatusService.GetSystemIntegrationsAsync(cancellationToken);
    }
}
