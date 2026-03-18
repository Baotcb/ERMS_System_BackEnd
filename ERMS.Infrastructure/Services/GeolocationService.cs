using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json.Serialization;
using ERMS.Application.Features.Geolocation.Queries.GetPublicGeolocation;
using ERMS.Application.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

namespace ERMS.Infrastructure.Services;

public sealed class GeolocationService : IGeolocationService
{
    private const string CacheKeyPrefix = "geo_";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMemoryCache _cache;

    public GeolocationService(
        HttpClient httpClient,
        IHttpContextAccessor httpContextAccessor,
        IMemoryCache cache)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _cache = cache;
    }

    public async Task<GetPublicGeolocationResponse> GetCurrentLocationAsync(CancellationToken cancellationToken)
    {
        var clientIp = GetClientIp();
        var cacheKey = $"{CacheKeyPrefix}{clientIp}";

        if (_cache.TryGetValue(cacheKey, out GetPublicGeolocationResponse? cachedResponse))
        {
            return cachedResponse!;
        }

        try
        {
            var response = await _httpClient.GetFromJsonAsync<IpApiResponse>(
                BuildRequestUri(clientIp),
                cancellationToken);

            if (response is null
                || !string.Equals(response.Status, "success", StringComparison.OrdinalIgnoreCase))
            {
                return CreateEmptyResponse();
            }

            var result = new GetPublicGeolocationResponse
            {
                City = response.City,
                RegionName = response.RegionName
            };

            _cache.Set(cacheKey, result, CacheDuration);
            return result;
        }
        catch
        {
            return CreateEmptyResponse();
        }
    }

    private string GetClientIp()
    {
        var remoteIpAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress;
        if (remoteIpAddress is null)
        {
            return "unknown";
        }

        if (remoteIpAddress.IsIPv4MappedToIPv6)
        {
            remoteIpAddress = remoteIpAddress.MapToIPv4();
        }

        return remoteIpAddress.ToString();
    }

    private static string BuildRequestUri(string clientIp)
    {
        var ipSegment = IsLocalIp(clientIp) ? string.Empty : clientIp;
        return $"json/{ipSegment}?fields=status,city,regionName&lang=vi";
    }

    private static bool IsLocalIp(string ipAddress)
    {
        if (!IPAddress.TryParse(ipAddress, out var parsedIp))
        {
            return true;
        }

        if (IPAddress.IsLoopback(parsedIp))
        {
            return true;
        }

        if (parsedIp.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return parsedIp.IsIPv6LinkLocal || parsedIp.IsIPv6SiteLocal;
        }

        var bytes = parsedIp.GetAddressBytes();
        return bytes[0] == 10
            || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            || (bytes[0] == 192 && bytes[1] == 168);
    }

    private static GetPublicGeolocationResponse CreateEmptyResponse() => new()
    {
        City = null,
        RegionName = null
    };

    private sealed class IpApiResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("regionName")]
        public string? RegionName { get; set; }
    }
}
