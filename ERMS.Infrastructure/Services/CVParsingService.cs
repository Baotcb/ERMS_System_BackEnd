using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ERMS.Application.Interface;
using ERMS.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ERMS.Infrastructure.Services;

public sealed class CVParsingService : ICVParsingService
{
    private const string CompletionsEndpoint = "https://api.groq.com/openai/v1/chat/completions";

    private readonly HttpClient _httpClient;
    private readonly ILogger<CVParsingService> _logger;
    private readonly GroqSettings _groqSettings;
    private readonly GroqModelSettings _groqModelSettings;

    public CVParsingService(
        HttpClient httpClient,
        ILogger<CVParsingService> logger,
        IOptions<GroqSettings> groqOptions,
        IOptions<GroqModelSettings> groqModelOptions)
    {
        _httpClient = httpClient;
        _logger = logger;
        _groqSettings = groqOptions.Value;
        _groqModelSettings = groqModelOptions.Value;

        if (string.IsNullOrWhiteSpace(_groqSettings.ApiKey))
        {
            throw new InvalidOperationException("Chưa cấu hình Groq ApiKey trong appsettings.");
        }
    }

    public async Task<CVParsedInfoDto> ParseCVAsync(string resumeText, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(resumeText))
        {
            return new CVParsedInfoDto();
        }

        try
        {
            var prompt = BuildPrompt(resumeText);
            var responseText = await SendChatCompletionAsync(prompt, cancellationToken);
            var jsonText = StripMarkdownCodeFences(responseText);

            var result = JsonSerializer.Deserialize<CVParseResponse>(jsonText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result == null)
            {
                return new CVParsedInfoDto();
            }

            return new CVParsedInfoDto
            {
                FullName = result.FullName?.Trim(),
                Email = result.Email?.Trim(),
                PhoneNumber = (result.PhoneNumber ?? result.Phone)?.Trim(),
                CurrentPosition = result.CurrentPosition?.Trim(),
                Skills = result.Skills ?? []
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể parse CV bằng Groq AI.");
            return new CVParsedInfoDto();
        }
    }

    private async Task<string> SendChatCompletionAsync(string prompt, CancellationToken cancellationToken)
    {
        var requestBody = new
        {
            model = GetRequiredModel(_groqModelSettings.CvParsing, "GroqModels:CvParsing"),
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            temperature = 0.1,
            response_format = new { type = "json_object" }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, CompletionsEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _groqSettings.ApiKey);
        request.Content = JsonContent.Create(requestBody);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"API Groq thất bại: {response.StatusCode} - {error}");
        }

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        var parsed = JsonSerializer.Deserialize<GroqChatResponse>(payload);
        var content = parsed?.Choices?.FirstOrDefault()?.Message?.Content;

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new Exception("Phản hồi parse CV từ Groq AI trống.");
        }

        return content;
    }

    private static string BuildPrompt(string resumeText)
    {
        return
            """
            Extract candidate information from the CV below.
            Return ONLY valid JSON with this shape:
            {
              "fullName": "string | null",
              "email": "string | null",
              "phoneNumber": "string | null",
              "currentPosition": "string | null",
              "skills": ["string"]
            }

            CV text:
            """
            + Environment.NewLine
            + resumeText;
    }

    private static string StripMarkdownCodeFences(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        var trimmed = text.Trim();
        if (!trimmed.StartsWith("```"))
        {
            return trimmed;
        }

        var firstNewline = trimmed.IndexOf('\n');
        if (firstNewline >= 0)
        {
            trimmed = trimmed[(firstNewline + 1)..];
        }

        var closingFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
        if (closingFence > 0)
        {
            trimmed = trimmed[..closingFence];
        }

        return trimmed.Trim();
    }

    private static string GetRequiredModel(string? configuredModel, string settingPath)
    {
        if (string.IsNullOrWhiteSpace(configuredModel))
        {
            throw new InvalidOperationException($"Thiếu cấu hình {settingPath} trong appsettings.");
        }

        return configuredModel.Trim();
    }

    private sealed class CVParseResponse
    {
        [JsonPropertyName("fullName")]
        public string? FullName { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("phoneNumber")]
        public string? PhoneNumber { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("currentPosition")]
        public string? CurrentPosition { get; set; }

        [JsonPropertyName("skills")]
        public List<string>? Skills { get; set; }
    }

    private sealed class GroqChatResponse
    {
        [JsonPropertyName("choices")]
        public List<GroqChoice>? Choices { get; set; }
    }

    private sealed class GroqChoice
    {
        [JsonPropertyName("message")]
        public GroqMessage? Message { get; set; }
    }

    private sealed class GroqMessage
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}
