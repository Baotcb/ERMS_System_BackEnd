using ERMS.Application.Interface;
using ERMS.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ERMS.Infrastructure.Services;

/// <summary>
/// Groq AI service implementing IAIService via the OpenAI-compatible Groq API.
/// API key loaded from appsettings via IOptions&lt;GroqSettings&gt;.
/// </summary>
public sealed class GroqAIService : IAIService
{
    private const string CompletionsEndpoint = "https://api.groq.com/openai/v1/chat/completions";

    private readonly HttpClient _httpClient;
    private readonly ILogger<GroqAIService> _logger;
    private readonly GroqSettings _settings;

    public GroqAIService(HttpClient httpClient, ILogger<GroqAIService> logger, IOptions<GroqSettings> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _settings = options.Value;

        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            throw new InvalidOperationException("Chưa cấu hình Groq ApiKey trong appsettings.");
    }

    public async Task<CVScreeningResultDto> AnalyzeResumeAsync(
        string resumeText,
        string jobDescription,
        string requiredSkills,
        string? educationLevel,
        string? experienceLevel)
    {
        var prompt = BuildPrompt(resumeText, jobDescription, requiredSkills, educationLevel, experienceLevel);

        _logger.LogInformation("Đang gửi yêu cầu phân tích CV đến Groq AI");

        try
        {
            var jsonText = await SendChatCompletionAsync(prompt, temperature: 0.2);
            jsonText = StripMarkdownCodeFences(jsonText);

            var result = JsonSerializer.Deserialize<CVScreeningResultDto>(jsonText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new Exception("Không thể phân tích phản hồi từ Groq AI");

            result.RawResponse = jsonText;

            _logger.LogInformation("Phân tích CV hoàn tất. Điểm tổng quan: {Score}", result.OverallScore);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Lỗi mạng khi gọi API Groq");
            throw new Exception("Không thể kết nối đến dịch vụ Groq AI. Vui lòng thử lại sau.", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Không thể phân tích phản hồi Groq AI dưới dạng JSON");
            throw new Exception("Groq AI trả về định dạng phản hồi không hợp lệ.", ex);
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
        var prompt = BuildJDPrompt(positionTitle, justification, requiredSkills, minExperience, maxExperience,
            educationLevel, salaryRangeMin, salaryRangeMax);

        _logger.LogInformation("Đang gửi yêu cầu tạo JD đến Groq AI cho vị trí: {PositionTitle}", positionTitle);

        for (var attempt = 1; attempt <= 2; attempt++)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var jsonText = await SendChatCompletionAsync(prompt, temperature: 0.7, cts.Token);
                jsonText = StripMarkdownCodeFences(jsonText);

                var result = JsonSerializer.Deserialize<GenerateJDResultDto>(jsonText, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? throw new Exception("Không thể phân tích phản hồi từ Groq AI");

                _logger.LogInformation("Tạo JD hoàn tất cho vị trí: {PositionTitle}", positionTitle);
                return result;
            }
            catch (HttpRequestException ex) when (attempt < 2)
            {
                _logger.LogWarning(ex, "Lỗi mạng khi tạo JD (lần {Attempt}), thử lại...", attempt);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Lỗi mạng khi gọi Groq AI để tạo JD");
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

    private async Task<string> SendChatCompletionAsync(
        string prompt,
        double temperature,
        CancellationToken cancellationToken = default)
    {
        var model = string.IsNullOrWhiteSpace(_settings.Model) ? "llama-3.3-70b-versatile" : _settings.Model;

        var requestBody = new
        {
            model,
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            temperature,
            response_format = new { type = "json_object" }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, CompletionsEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        request.Content = JsonContent.Create(requestBody);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Lỗi API Groq: {StatusCode} - {Error}", response.StatusCode, errorContent);
            throw new HttpRequestException($"Yêu cầu API Groq thất bại: {response.StatusCode}");
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var groqResponse = JsonSerializer.Deserialize<GroqChatResponse>(responseContent);
        var content = groqResponse?.Choices?.FirstOrDefault()?.Message?.Content;

        if (string.IsNullOrEmpty(content))
            throw new Exception("Phản hồi từ Groq AI trống");

        return content;
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
            Bạn là chuyên gia nhân sự. Tạo mô tả công việc bằng tiếng Việt dựa trên thông tin sau.

            **BẮT BUỘC: Toàn bộ nội dung phải viết bằng tiếng Việt.**

            ## Thông tin vị trí:
            - Tên vị trí: {{positionTitle}}
            - Bối cảnh/Lý do: {{justification ?? "Không có"}}
            - Kỹ năng yêu cầu: {{requiredSkills ?? "Không xác định"}}
            - Kinh nghiệm: {{experienceText}}
            - Trình độ học vấn: {{educationLevel ?? "Không xác định"}}
            - Mức lương: {{salaryText}}

            ## Yêu cầu:
            Tạo 2 phần:

            1. **description** (Mô tả công việc): Viết dạng gạch đầu dòng (mỗi dòng bắt đầu bằng "• ").
               - Liệt kê 4-6 đầu mục công việc chính, ngắn gọn, mỗi dòng 1 câu.
               - Dựa trên tên vị trí và kỹ năng yêu cầu để suy ra công việc cụ thể.
               - Cuối cùng thêm 1 dòng: "• Thực hiện công việc khác theo sự phân công của Ban giám đốc."

            2. **requirements** (Yêu cầu ứng viên): Viết dạng gạch đầu dòng (mỗi dòng bắt đầu bằng "• ").
               - Liệt kê 3-5 yêu cầu ngắn gọn, mỗi yêu cầu là 1 câu đơn giản dễ hiểu.
               - Bao gồm: trình độ học vấn, kinh nghiệm, kỹ năng chuyên môn, phẩm chất cá nhân.

            3. **benefits**: Trả về chuỗi rỗng "". KHÔNG tạo nội dung quyền lợi.

            **Không tạo nội dung phân biệt đối xử hoặc gây hiểu lầm.**

            Trả về JSON duy nhất (không thêm text khác):
            {
                "description": "• ...\n• ...\n• ...",
                "requirements": "• ...\n• ...\n• ...",
                "benefits": ""
            }
            """;
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

    /// <summary>
    /// Strips markdown code fences from JSON response if present.
    /// Handles: ```json {...} ```, ``` {...} ```, or plain {...}
    /// </summary>
    private static string StripMarkdownCodeFences(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var trimmed = text.Trim();

        if (trimmed.StartsWith("```"))
        {
            var firstNewline = trimmed.IndexOf('\n');
            if (firstNewline > 0)
                trimmed = trimmed[(firstNewline + 1)..];
            else
                trimmed = trimmed[3..];

            var closingFence = trimmed.LastIndexOf("```");
            if (closingFence > 0)
                trimmed = trimmed[..closingFence];
        }

        return trimmed.Trim();
    }

    #region Groq API Response Models

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

    #endregion
}
