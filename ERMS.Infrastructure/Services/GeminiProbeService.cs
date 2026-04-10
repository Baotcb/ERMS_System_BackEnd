using System.Net.Http.Json;
using System.Text.Json;
using ERMS.Application.Features.Diagnostics.Queries.GetGeminiProbe;
using ERMS.Application.Interface;
using ERMS.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace ERMS.Infrastructure.Services;

public sealed class GeminiProbeService : IGeminiProbeService
{
    private const int MaxPreviewLength = 800;
    private readonly HttpClient _httpClient;
    private readonly GeminiSettings _settings;

    public GeminiProbeService(HttpClient httpClient, IOptions<GeminiSettings> options)
    {
        _httpClient = httpClient;
        _settings = options.Value;
    }

    public async Task<GetGeminiProbeResponse> ProbeAsync(CancellationToken cancellationToken)
    {
        var checkedAtUtc = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            return new GetGeminiProbeResponse
            {
                Success = false,
                HttpStatusCode = 0,
                GoogleStatus = "CONFIGURATION_ERROR",
                Message = "Gemini API key is not configured.",
                CheckedAtUtc = checkedAtUtc
            };
        }

        var model = string.IsNullOrWhiteSpace(_settings.Model) ? "gemini-2.5-flash" : _settings.Model;
        var requestUrl = $"https://erms-gemini-proxy.baotcq1511.workers.dev/v1beta/models/{model}:generateContent?key={_settings.ApiKey}";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = "Reply with the single word OK." }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0
            }
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(requestUrl, requestBody, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return new GetGeminiProbeResponse
                {
                    Success = true,
                    HttpStatusCode = (int)response.StatusCode,
                    GoogleStatus = response.StatusCode.ToString(),
                    Message = "Gemini probe succeeded.",
                    ResponsePreview = Truncate(responseContent),
                    CheckedAtUtc = checkedAtUtc
                };
            }

            var (googleStatus, message) = ExtractErrorDetails(responseContent);

            return new GetGeminiProbeResponse
            {
                Success = false,
                HttpStatusCode = (int)response.StatusCode,
                GoogleStatus = googleStatus,
                Message = message,
                ResponsePreview = Truncate(responseContent),
                CheckedAtUtc = checkedAtUtc
            };
        }
        catch (Exception ex)
        {
            return new GetGeminiProbeResponse
            {
                Success = false,
                HttpStatusCode = 0,
                GoogleStatus = "EXCEPTION",
                Message = ex.Message,
                CheckedAtUtc = checkedAtUtc
            };
        }
    }

    private static (string? status, string? message) ExtractErrorDetails(string responseContent)
    {
        try
        {
            using var document = JsonDocument.Parse(responseContent);

            if (!document.RootElement.TryGetProperty("error", out var errorElement))
            {
                return ("HTTP_ERROR", "Gemini probe failed.");
            }

            var status = errorElement.TryGetProperty("status", out var statusElement)
                ? statusElement.GetString()
                : "HTTP_ERROR";

            var message = errorElement.TryGetProperty("message", out var messageElement)
                ? messageElement.GetString()
                : "Gemini probe failed.";

            return (status, message);
        }
        catch
        {
            return ("HTTP_ERROR", "Gemini probe failed.");
        }
    }

    private static string Truncate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return value.Length <= MaxPreviewLength
            ? value
            : value[..MaxPreviewLength];
    }
}
