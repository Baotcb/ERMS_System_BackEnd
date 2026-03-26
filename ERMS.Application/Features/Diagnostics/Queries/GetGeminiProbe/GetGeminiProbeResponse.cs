using System.Text.Json.Serialization;

namespace ERMS.Application.Features.Diagnostics.Queries.GetGeminiProbe;

public sealed class GetGeminiProbeResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("httpStatusCode")]
    public int HttpStatusCode { get; set; }

    [JsonPropertyName("googleStatus")]
    public string? GoogleStatus { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("responsePreview")]
    public string? ResponsePreview { get; set; }

    [JsonPropertyName("checkedAtUtc")]
    public DateTime CheckedAtUtc { get; set; }
}
