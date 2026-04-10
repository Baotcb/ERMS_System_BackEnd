using ERMS.Application.Interface;
using ERMS.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace ERMS.Infrastructure.Services;

/// <summary>
/// Extracts candidate contact information from CV text using Gemini AI
/// </summary>
public class CvInfoExtractorService : ICvInfoExtractorService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CvInfoExtractorService> _logger;
    private readonly GeminiSettings _settings;
    private readonly string _geminiApiUrl;

    public CvInfoExtractorService(
        HttpClient httpClient,
        ILogger<CvInfoExtractorService> logger,
        IOptions<GeminiSettings> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _settings = options.Value;

        var model = string.IsNullOrWhiteSpace(_settings.Model) ? "gemini-2.5-flash" : _settings.Model;
        _geminiApiUrl = $"https://erms-gemini-proxy.baotcq1511.workers.dev/v1beta/models/{model}:generateContent";
    }

    public async Task<ExtractedCvInfoDto> ExtractContactInfoAsync(
        string resumeText,
        byte[]? pdfBytes = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedText = resumeText?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedText))
        {
            return await ExtractFromPdfAsync(pdfBytes, cancellationToken);
        }

        var extractedFromText = await ExtractFromTextAsync(normalizedText, cancellationToken);
        if (!NeedsPdfFallback(extractedFromText) || pdfBytes is null || pdfBytes.Length == 0)
        {
            return extractedFromText;
        }

        _logger.LogInformation("CV text extraction did not yield contact info. Falling back to PDF multimodal extraction.");

        var extractedFromPdf = await ExtractFromPdfAsync(pdfBytes, cancellationToken);
        return MergeResults(extractedFromText, extractedFromPdf);
    }

    private async Task<ExtractedCvInfoDto> ExtractFromTextAsync(string resumeText, CancellationToken cancellationToken)
    {
        // Limit text to avoid large prompt costs
        var truncatedText = resumeText.Length > 5000 ? resumeText[..5000] : resumeText;

        var prompt = $$"""
            Extract contact information from the following resume text.
            Return a JSON object with these fields: fullName, email, phone.
            If a field cannot be found, return null for that field.
            Do not guess or fabricate information.
            
            Resume text:
            {{truncatedText}}
            
            Return JSON only, no additional text:
            {"fullName": "...", "email": "...", "phone": "..."}
            """;

        var requestBody = CreateRequestBody(new object[] { new { text = prompt } });
        return await SendRequestAsync(requestBody, cancellationToken);
    }

    private async Task<ExtractedCvInfoDto> ExtractFromPdfAsync(byte[]? pdfBytes, CancellationToken cancellationToken)
    {
        if (pdfBytes is null || pdfBytes.Length == 0)
        {
            return new ExtractedCvInfoDto();
        }

        var prompt = """
            Extract contact information from this resume PDF.
            Return a JSON object with these fields: fullName, email, phone.
            If a field cannot be found, return null for that field.
            Do not guess or fabricate information.

            Return JSON only, no additional text:
            {"fullName": "...", "email": "...", "phone": "..."}
            """;

        var requestBody = CreateRequestBody(new object[]
        {
            new
            {
                inline_data = new
                {
                    mime_type = "application/pdf",
                    data = Convert.ToBase64String(pdfBytes)
                }
            },
            new { text = prompt }
        });

        return await SendRequestAsync(requestBody, cancellationToken);
    }

    private object CreateRequestBody(object[] parts)
    {
        return new
        {
            contents = new[]
            {
                new { parts }
            },
            generationConfig = new
            {
                responseMimeType = "application/json",
                temperature = 0.1
            }
        };
    }

    private async Task<ExtractedCvInfoDto> SendRequestAsync(object requestBody, CancellationToken cancellationToken)
    {
        var requestUrl = $"{_geminiApiUrl}?key={_settings.ApiKey}";

        try
        {
            var response = await _httpClient.PostAsJsonAsync(requestUrl, requestBody, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gemini CV info extraction failed: {StatusCode}", response.StatusCode);
                return new ExtractedCvInfoDto();
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var jsonText = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
            if (string.IsNullOrWhiteSpace(jsonText))
                return new ExtractedCvInfoDto();

            jsonText = StripMarkdownCodeFences(jsonText);

            return JsonSerializer.Deserialize<ExtractedCvInfoDto>(jsonText,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new ExtractedCvInfoDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi trích xuất thông tin CV");
            return new ExtractedCvInfoDto();
        }
    }

    private static bool NeedsPdfFallback(ExtractedCvInfoDto extracted)
    {
        return string.IsNullOrWhiteSpace(extracted.FullName)
            && string.IsNullOrWhiteSpace(extracted.Email)
            && string.IsNullOrWhiteSpace(extracted.Phone);
    }

    private static ExtractedCvInfoDto MergeResults(ExtractedCvInfoDto primary, ExtractedCvInfoDto fallback)
    {
        return new ExtractedCvInfoDto
        {
            FullName = string.IsNullOrWhiteSpace(primary.FullName) ? fallback.FullName : primary.FullName,
            Email = string.IsNullOrWhiteSpace(primary.Email) ? fallback.Email : primary.Email,
            Phone = string.IsNullOrWhiteSpace(primary.Phone) ? fallback.Phone : primary.Phone
        };
    }

    private static string StripMarkdownCodeFences(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.StartsWith("```"))
        {
            var firstNewline = trimmed.IndexOf('\n');
            if (firstNewline >= 0)
                trimmed = trimmed[(firstNewline + 1)..];
            if (trimmed.EndsWith("```"))
                trimmed = trimmed[..^3].TrimEnd();
        }
        return trimmed;
    }

    // Reuse GeminiAIService's nested response model via a local alias
    private sealed class GeminiResponse
    {
        public List<Candidate>? Candidates { get; set; }
        public sealed class Candidate { public Content? Content { get; set; } }
        public sealed class Content { public List<Part>? Parts { get; set; } }
        public sealed class Part { public string? Text { get; set; } }
    }
}
