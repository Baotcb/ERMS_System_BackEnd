using MediatR;

namespace ERMS.Application.Features.Admin.Queries.GetSystemIntegrations;

public sealed class GetSystemIntegrationsHandler : IRequestHandler<GetSystemIntegrationsQuery, GetSystemIntegrationsResponse>
{
    public GetSystemIntegrationsHandler()
    {
    }

    public Task<GetSystemIntegrationsResponse> Handle(GetSystemIntegrationsQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new GetSystemIntegrationsResponse
        {
            new()
            {
                Name = "Google OAuth",
                Category = "Authentication",
                Status = "Configured",
                EnvironmentScope = "System",
                LastChecked = null
            },
            new()
            {
                Name = "SMTP",
                Category = "Communication",
                Status = "Configured",
                EnvironmentScope = "System",
                LastChecked = null
            },
            new()
            {
                Name = "Cloudinary",
                Category = "Media",
                Status = "Configured",
                EnvironmentScope = "System",
                LastChecked = null
            },
            new()
            {
                Name = "Gemini",
                Category = "AI",
                Status = "Configured",
                EnvironmentScope = "System",
                LastChecked = null
            }
        });
    }
}
