using ERMS.Application.Features.Admin.Queries.GetSystemIntegrations;
using ERMS.Application.Interface;

namespace ERMS.Infrastructure.Services;

public sealed class SystemIntegrationStatusService : ISystemIntegrationStatusService
{
    private const string ConfiguredStatus = "Configured";
    private const string MissingConfigurationStatus = "MissingConfiguration";

    private readonly IAIServiceConfiguration _aiServiceConfiguration;

    public SystemIntegrationStatusService(IAIServiceConfiguration aiServiceConfiguration)
    {
        _aiServiceConfiguration = aiServiceConfiguration;
    }

    public Task<GetSystemIntegrationsResponse> GetSystemIntegrationsAsync(CancellationToken cancellationToken)
    {
        var checkedAt = DateTime.UtcNow;

        return Task.FromResult(new GetSystemIntegrationsResponse
        {
            BuildIntegration("Groq", "AI", _aiServiceConfiguration.HasApiKey, checkedAt)
        });
    }

    private static SystemIntegrationDto BuildIntegration(string name, string category, bool isConfigured, DateTime checkedAt)
    {
        return new SystemIntegrationDto
        {
            Name = name,
            Category = category,
            Status = isConfigured ? ConfiguredStatus : MissingConfigurationStatus,
            EnvironmentScope = "System",
            LastChecked = checkedAt
        };
    }
}
