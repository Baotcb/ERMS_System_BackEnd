using System.Text.Json.Serialization;

namespace ERMS.Application.Features.Geolocation.Queries.GetPublicGeolocation;

public sealed class GetPublicGeolocationResponse
{
    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("regionName")]
    public string? RegionName { get; set; }
}
