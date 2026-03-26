using System.Text.Json.Serialization;

namespace ERMS.Application.Features.Diagnostics.Queries.GetBackendEgressIp;

public sealed class GetBackendEgressIpResponse
{
    [JsonPropertyName("publicIp")]
    public string PublicIp { get; set; } = string.Empty;

    [JsonPropertyName("provider")]
    public string Provider { get; set; } = string.Empty;

    [JsonPropertyName("checkedAtUtc")]
    public DateTime CheckedAtUtc { get; set; }
}
