namespace ERMS.Application.Features.Admin.Queries.GetSystemIntegrations;

public sealed class GetSystemIntegrationsResponse : List<SystemIntegrationDto>
{
}

public sealed class SystemIntegrationDto
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string EnvironmentScope { get; set; } = string.Empty;
    public DateTime? LastChecked { get; set; }
}
