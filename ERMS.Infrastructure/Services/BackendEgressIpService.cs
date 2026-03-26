using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ERMS.Application.Features.Diagnostics.Queries.GetBackendEgressIp;
using ERMS.Application.Interface;

namespace ERMS.Infrastructure.Services;

public sealed class BackendEgressIpService : IBackendEgressIpService
{
    private const string ProviderName = "api.ipify.org";
    private readonly HttpClient _httpClient;

    public BackendEgressIpService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<GetBackendEgressIpResponse> GetPublicEgressIpAsync(CancellationToken cancellationToken)
    {
        var payload = await _httpClient.GetFromJsonAsync<IpifyResponse>("?format=json", cancellationToken);

        if (string.IsNullOrWhiteSpace(payload?.Ip))
        {
            throw new InvalidOperationException("Could not determine backend public egress IP.");
        }

        return new GetBackendEgressIpResponse
        {
            PublicIp = payload.Ip,
            Provider = ProviderName,
            CheckedAtUtc = DateTime.UtcNow
        };
    }

    private sealed class IpifyResponse
    {
        [JsonPropertyName("ip")]
        public string? Ip { get; set; }
    }
}
