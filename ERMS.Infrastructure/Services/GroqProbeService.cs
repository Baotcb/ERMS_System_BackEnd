using ERMS.Application.Features.Diagnostics.Queries.GetGeminiProbe;
using ERMS.Application.Interface;
using ERMS.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ERMS.Infrastructure.Services;

public sealed class GroqProbeService : IAIProbeService
{
    private const string CompletionsEndpoint = "https://api.groq.com/openai/v1/chat/completions";
    private const int MaxPreviewLength = 800;

    private readonly HttpClient _httpClient;
    private readonly GroqSettings _settings;
    private readonly GroqModelSettings _modelSettings;

    public GroqProbeService(
        HttpClient httpClient,
        IOptions<GroqSettings> options,
        IOptions<GroqModelSettings> modelOptions)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _modelSettings = modelOptions.Value;
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
                Message = "Groq API key chưa được cấu hình.",
                CheckedAtUtc = checkedAtUtc
            };
        }

        var model = ResolveModel();
        if (string.IsNullOrWhiteSpace(model))
        {
            return new GetGeminiProbeResponse
            {
                Success = false,
                HttpStatusCode = 0,
                GoogleStatus = "CONFIGURATION_ERROR",
                Message = "Model Groq chưa được cấu hình (GroqModels:Probe).",
                CheckedAtUtc = checkedAtUtc
            };
        }

        var requestBody = new
        {
            model,
            messages = new[]
            {
                new { role = "user", content = "Reply with the single word OK." }
            },
            temperature = 0,
            max_tokens = 10
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, CompletionsEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
            request.Content = JsonContent.Create(requestBody);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return new GetGeminiProbeResponse
                {
                    Success = true,
                    HttpStatusCode = (int)response.StatusCode,
                    GoogleStatus = response.StatusCode.ToString(),
                    Message = "Kiểm tra kết nối Groq thành công.",
                    ResponsePreview = Truncate(responseContent),
                    CheckedAtUtc = checkedAtUtc
                };
            }

            var (status, message) = ExtractErrorDetails(responseContent);

            return new GetGeminiProbeResponse
            {
                Success = false,
                HttpStatusCode = (int)response.StatusCode,
                GoogleStatus = status,
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
                return ("HTTP_ERROR", "Kiểm tra kết nối Groq thất bại.");

            var status = errorElement.TryGetProperty("type", out var typeElement)
                ? typeElement.GetString()
                : "HTTP_ERROR";

            var message = errorElement.TryGetProperty("message", out var messageElement)
                ? messageElement.GetString()
                : "Kiểm tra kết nối Groq thất bại.";

            return (status, message);
        }
        catch
        {
            return ("HTTP_ERROR", "Kiểm tra kết nối Groq thất bại.");
        }
    }

    private static string Truncate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        return value.Length <= MaxPreviewLength ? value : value[..MaxPreviewLength];
    }

    private string? ResolveModel()
    {
        if (!string.IsNullOrWhiteSpace(_modelSettings.Probe))
        {
            return _modelSettings.Probe.Trim();
        }

        return null;
    }
}
