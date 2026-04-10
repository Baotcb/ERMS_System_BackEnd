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
            throw new InvalidOperationException("Chưa cấu hình Gemini ApiKey trong appsettings.");
            
        var model = string.IsNullOrWhiteSpace(_settings.Model) ? "gemini-2.5-flash" : _settings.Model;
        _geminiApiUrl = $"https://erms-gemini-proxy.baotcq1511.workers.dev/v1beta/models/{model}:generateContent";
    }

    public async Task<CVScreeningResultDto> AnalyzeResumeAsync(
        string resumeText,
        string jobDescription,
        string requiredSkills,
        string? educationLevel,
        string? experienceLevel)
    {
        var prompt = BuildPrompt(resumeText, jobDescription, requiredSkills, educationLevel, experienceLevel);

        _logger.LogInformation("Đang gửi yêu cầu phân tích CV đến Gemini AI");

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
                _logger.LogError("Lỗi API Gemini: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new Exception($"Yêu cầu API Gemini thất bại: {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Phản hồi API Gemini: {Response}", responseContent);
            Console.WriteLine("===== RAW GEMINI RESPONSE =====");
            Console.WriteLine(responseContent);
            Console.WriteLine("===============================");

            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent);
            var jsonText = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

            if (string.IsNullOrEmpty(jsonText))
            {
                throw new Exception("Phản hồi từ Gemini AI trống");
            }

            // Strip markdown code fences if present (Gemini sometimes wraps JSON in ```json ... ```)
            jsonText = StripMarkdownCodeFences(jsonText);

            var result = JsonSerializer.Deserialize<CVScreeningResultDto>(jsonText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new Exception("Không thể phân tích phản hồi từ Gemini AI");

            result.RawResponse = responseContent;

            _logger.LogInformation("Phân tích CV hoàn tất. Điểm tổng quan: {Score}", result.OverallScore);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Lỗi mạng khi gọi API Gemini");
            throw new Exception("Không thể kết nối đến dịch vụ Gemini AI. Vui lòng thử lại sau.", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Không thể phân tích phản hồi Gemini AI dưới dạng JSON");
            throw new Exception("Gemini AI trả về định dạng phản hồi không hợp lệ.", ex);
        }
    }

    public async Task<GenerateJDResultDto> GenerateJobDescriptionAsync(
        string positionTitle,
        string? justification,
        string? requiredSkills,
        int? minExperience,
        int? maxExperience,
        string? educationLevel,
        decimal? salaryRangeMin,
        decimal? salaryRangeMax)
    {
        var prompt = BuildJDPrompt(positionTitle, justification, requiredSkills, minExperience, maxExperience, educationLevel, salaryRangeMin, salaryRangeMax);

        _logger.LogInformation("Đang gửi yêu cầu tạo JD đến Gemini AI cho vị trí: {PositionTitle}", positionTitle);

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
                temperature = 0.7
            }
        };

        var requestUrl = $"{_geminiApiUrl}?key={_settings.ApiKey}";

        for (var attempt = 1; attempt <= 2; attempt++)
        {
            try
            {
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(30));
                var response = await _httpClient.PostAsJsonAsync(requestUrl, requestBody, cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Lỗi API Gemini khi tạo JD: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    throw new Exception($"Yêu cầu API Gemini thất bại: {response.StatusCode}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent);
                var jsonText = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

                if (string.IsNullOrEmpty(jsonText))
                    throw new Exception("Phản hồi từ Gemini AI trống");

                jsonText = StripMarkdownCodeFences(jsonText);

                var result = JsonSerializer.Deserialize<GenerateJDResultDto>(jsonText, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? throw new Exception("Không thể phân tích phản hồi từ Gemini AI");

                _logger.LogInformation("Tạo JD hoàn tất cho vị trí: {PositionTitle}", positionTitle);
                return result;
            }
            catch (HttpRequestException ex) when (attempt < 2)
            {
                _logger.LogWarning(ex, "Lỗi mạng khi tạo JD (lần {Attempt}), thử lại...", attempt);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Lỗi mạng khi gọi Gemini AI để tạo JD");
                throw new Exception("Không thể tạo JD. Vui lòng thử lại sau.", ex);
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                _logger.LogError(ex, "Lỗi khi tạo JD");
                throw new Exception("Không thể tạo JD. Vui lòng thử lại sau.", ex);
            }
        }

        throw new Exception("Không thể tạo JD. Vui lòng thử lại sau.");
    }

    private static string BuildJDPrompt(
        string positionTitle,
        string? justification,
        string? requiredSkills,
        int? minExperience,
        int? maxExperience,
        string? educationLevel,
        decimal? salaryRangeMin,
        decimal? salaryRangeMax)
    {
        var experienceText = (minExperience.HasValue || maxExperience.HasValue)
            ? $"{minExperience ?? 0}-{maxExperience ?? minExperience} năm"
            : "Không xác định";

        var salaryText = (salaryRangeMin.HasValue || salaryRangeMax.HasValue)
            ? $"{salaryRangeMin?.ToString("N0") ?? "?"} - {salaryRangeMax?.ToString("N0") ?? "?"} VND"
            : "Thỏa thuận";

        return $$"""
            You are an expert HR professional and job description writer.
            Generate a professional job description in Vietnamese based on the following position details.

            **CRITICAL: ALL output text MUST be written entirely in Vietnamese.**

            ## Position Details:
            - Position Title: {{positionTitle}}
            - Justification/Context: {{justification ?? "Không có"}}
            - Required Skills: {{requiredSkills ?? "Không xác định"}}
            - Experience: {{experienceText}}
            - Education Level: {{educationLevel ?? "Không xác định"}}
            - Salary Range: {{salaryText}}

            ## Instructions:
            Generate a structured job description with three sections. Be specific, professional, and compelling.
            - description: 400-800 characters. Company introduction + role overview + key responsibilities.
            - requirements: 300-600 characters. Technical skills, experience requirements, education, soft skills.
            - benefits: 200-500 characters. Salary, insurance, training, career growth, work environment.

            **Do not generate discriminatory, illegal, or misleading content.**

            Return your output in the following JSON format ONLY (no additional text):
            {
                "description": "...",
                "requirements": "...",
                "benefits": "..."
            }
            """;
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
                "strengths": ["điểm mạnh 1", "điểm mạnh 2"],
                "concerns": ["điểm lo ngại 1", "điểm lo ngại 2"],
                "summary": "Tóm tắt ngắn gọn 2-3 câu về mức độ phù hợp của ứng viên với vị trí này"
            }
            """;

        return $"""
            You are an expert HR recruiter and resume analyst. Analyze the following resume against the job requirements and provide a detailed scoring.
            **CRITICAL: ALL text output in the JSON (matchedSkills, missingSkills, strengths, concerns, summary) MUST be written entirely in Vietnamese.**

            ## Job Requirements:
            **Description:** {jobDescription}
            **Required Skills:** {requiredSkills}
            **Education Level:** {educationLevel ?? "Không xác định"}
            **Experience Level:** {experienceLevel ?? "Không xác định"}

            ## Candidate Resume:
            {resumeText}

            ## Instructions:
            Analyze the resume and provide scores from 0 to 100 for each category. Be objective and fair.
            **ALL text fields (matchedSkills, missingSkills, strengths, concerns, summary) MUST be in Vietnamese.**

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
