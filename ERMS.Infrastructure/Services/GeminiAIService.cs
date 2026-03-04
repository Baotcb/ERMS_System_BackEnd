using ERMS.Application.Interface;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ERMS.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace ERMS.Infrastructure.Services;

/// <summary>
/// Gemini AI service using direct REST API calls with HttpClient
/// API key and Model loaded from appsettings via IOptions
/// </summary>
public class GeminiAIService : IGeminiAIService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiAIService> _logger;
    private readonly GeminiSettings _settings;
    private readonly string _geminiApiUrl;

    public GeminiAIService(HttpClient httpClient, ILogger<GeminiAIService> logger, IOptions<GeminiSettings> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _settings = options.Value;

        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            throw new InvalidOperationException("Gemini ApiKey is not configured in appsettings.");
            
        var model = string.IsNullOrWhiteSpace(_settings.Model) ? "gemini-2.5-flash" : _settings.Model;
        _geminiApiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";
    }

    public async Task<CVScreeningResultDto> AnalyzeResumeAsync(
        string resumeText,
        string jobDescription,
        string requiredSkills,
        string? educationLevel,
        string? experienceLevel)
    {
        var prompt = BuildPrompt(resumeText, jobDescription, requiredSkills, educationLevel, experienceLevel);

        _logger.LogInformation("Sending resume analysis request to Gemini AI");

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                responseMimeType = "application/json",
                temperature = 0.2
            }
        };

        var requestUrl = $"{_geminiApiUrl}?key={_settings.ApiKey}";

        try
        {
            var response = await _httpClient.PostAsJsonAsync(requestUrl, requestBody);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Gemini API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new Exception($"Gemini API request failed: {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Gemini API response: {Response}", responseContent);

            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent);
            var jsonText = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

            if (string.IsNullOrEmpty(jsonText))
            {
                throw new Exception("Empty response from Gemini AI");
            }

            // Strip markdown code fences if present (Gemini sometimes wraps JSON in ```json ... ```)
            jsonText = StripMarkdownCodeFences(jsonText);

            var result = JsonSerializer.Deserialize<CVScreeningResultDto>(jsonText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new Exception("Failed to parse Gemini AI response");

            result.RawResponse = responseContent;

            _logger.LogInformation("Resume analysis complete. Overall score: {Score}", result.OverallScore);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error calling Gemini API");
            throw new Exception("Failed to connect to Gemini AI service. Please try again later.", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Gemini AI response as JSON");
            throw new Exception("Gemini AI returned invalid response format.", ex);
        }
    }

    /// <summary>
    /// Strips markdown code fences from JSON response if present
    /// Handles: ```json {...} ```, ``` {...} ```, or plain {...}
    /// </summary>
    private static string StripMarkdownCodeFences(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var trimmed = text.Trim();

        // Pattern: ```json\n{...}\n``` or ```\n{...}\n```
        if (trimmed.StartsWith("```"))
        {
            // Find the first newline after opening fence
            var firstNewline = trimmed.IndexOf('\n');
            if (firstNewline > 0)
            {
                trimmed = trimmed[(firstNewline + 1)..];
            }
            else
            {
                // No newline, strip just the backticks
                trimmed = trimmed[3..];
            }

            // Find and remove closing fence
            var closingFence = trimmed.LastIndexOf("```");
            if (closingFence > 0)
            {
                trimmed = trimmed[..closingFence];
            }
        }

        return trimmed.Trim();
    }

    private static string BuildPrompt(
        string resumeText,
        string jobDescription,
        string requiredSkills,
        string? educationLevel,
        string? experienceLevel)
    {
        var jsonTemplate = """
            {
                "overallScore": <number 0-100>,
                "skillMatchScore": <number 0-100>,
                "experienceMatchScore": <number 0-100>,
                "educationMatchScore": <number 0-100>,
                "keywordMatchScore": <number 0-100>,
                "matchedSkills": ["skill1", "skill2"],
                "missingSkills": ["skill1", "skill2"],
                "strengths": ["strength1", "strength2"],
                "concerns": ["concern1", "concern2"],
                "summary": "Brief 2-3 sentence summary of the candidate's fit for this role"
            }
            """;

        return $"""
            You are an expert HR recruiter and resume analyst. Analyze the following resume against the job requirements and provide a detailed scoring.

            ## Job Requirements:
            **Description:** {jobDescription}
            **Required Skills:** {requiredSkills}
            **Education Level:** {educationLevel ?? "Not specified"}
            **Experience Level:** {experienceLevel ?? "Not specified"}

            ## Candidate Resume:
            {resumeText}

            ## Instructions:
            Analyze the resume and provide scores from 0 to 100 for each category. Be objective and fair.

            Return your analysis in the following JSON format ONLY (no additional text):
            {jsonTemplate}
            """;
    }

    #region Gemini API Response Models

    private class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public List<Candidate>? Candidates { get; set; }
    }

    private class Candidate
    {
        [JsonPropertyName("content")]
        public Content? Content { get; set; }
    }

    private class Content
    {
        [JsonPropertyName("parts")]
        public List<Part>? Parts { get; set; }
    }

    private class Part
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    #endregion
}
