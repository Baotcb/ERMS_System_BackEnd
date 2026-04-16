using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Organization;
using ERMS.Domain.Entities.Training;
using ERMS.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ERMS.Infrastructure.Services;

public sealed class SkillGapChatService : ISkillGapChatService
{
    private const string CompletionsEndpoint = "https://api.groq.com/openai/v1/chat/completions";
    private const int MaxEmployeesInPrompt = 30;
    private const int MaxHistoryMessages = 20;

    private readonly HttpClient _httpClient;
    private readonly ILogger<SkillGapChatService> _logger;
    private readonly GroqSettings _groqSettings;
    private readonly GroqModelSettings _modelSettings;
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public SkillGapChatService(
        HttpClient httpClient,
        ILogger<SkillGapChatService> logger,
        IOptions<GroqSettings> groqOptions,
        IOptions<GroqModelSettings> modelOptions,
        IERMSDbContext context,
        ICurrentUserService currentUserService)
    {
        _httpClient = httpClient;
        _logger = logger;
        _groqSettings = groqOptions.Value;
        _modelSettings = modelOptions.Value;
        _context = context;
        _currentUserService = currentUserService;

        if (string.IsNullOrWhiteSpace(_groqSettings.ApiKey))
        {
            throw new InvalidOperationException("Chưa cấu hình Groq ApiKey trong appsettings.");
        }

        if (string.IsNullOrWhiteSpace(_modelSettings.TrainingSuggestion))
        {
            throw new InvalidOperationException("Thiếu cấu hình model GroqModels:TrainingSuggestion.");
        }
    }

    public async Task<SkillGapChatResponseDto> AskAsync(
        SkillGapChatRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new ArgumentException("Nội dung tin nhắn không được để trống.", nameof(request.Message));
        }

        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync()
            ?? throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào.");

        var departmentId = await _currentUserService.GetDepartmentIdAsync()
            ?? throw new UnauthorizedAccessException("Người dùng chưa được gán phòng ban.");

        var conversation = await GetOrCreateConversationAsync(
            request.ConversationId,
            enterpriseId,
            userId,
            departmentId,
            cancellationToken);

        var historyMessages = await _context.ChatMessages
            .Where(m => m.ChatConversationId == conversation.Id && !m.IsDeleted)
            .OrderByDescending(m => m.CreatedAt)
            .Take(MaxHistoryMessages)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

        var employees = await _context.Employees
            .Where(e =>
                e.EnterpriseId == enterpriseId &&
                e.DepartmentId == departmentId &&
                !e.IsDeleted)
            .Include(e => e.User)
            .OrderBy(e => e.User.FullName)
            .Take(MaxEmployeesInPrompt)
            .ToListAsync(cancellationToken);

        var courses = await _context.Courses
            .Where(c => c.EnterpriseId == enterpriseId && !c.IsDeleted)
            .Include(c => c.CourseSkills)
                .ThenInclude(cs => cs.Skill)
            .OrderBy(c => c.CourseName)
            .ToListAsync(cancellationToken);

        var departmentName = await _context.Departments
            .Where(d => d.Id == departmentId && d.EnterpriseId == enterpriseId && !d.IsDeleted)
            .Select(d => d.DepartmentName)
            .FirstOrDefaultAsync(cancellationToken)
            ?? $"Phòng ban #{departmentId}";

        var systemPrompt = BuildSystemPrompt(departmentName, employees, courses);
        var completionMessage = await SendCompletionAsync(systemPrompt, historyMessages, request.Message, cancellationToken);
        var parsedResponse = ParseAssistantResponse(completionMessage);

        var now = DateTime.UtcNow;
        var userMessage = new ChatMessage
        {
            Id = Guid.CreateVersion7(),
            ChatConversationId = conversation.Id,
            EnterpriseId = enterpriseId,
            UserId = userId,
            Role = "user",
            Content = request.Message.Trim(),
            IsDeleted = false,
            CreatedAt = now
        };

        var fullAssistantContent = FormatAssistantContent(parsedResponse);

        var assistantMessage = new ChatMessage
        {
            Id = Guid.CreateVersion7(),
            ChatConversationId = conversation.Id,
            EnterpriseId = enterpriseId,
            UserId = userId,
            Role = "assistant",
            Content = fullAssistantContent,
            IsDeleted = false,
            CreatedAt = now.AddMilliseconds(1)
        };

        _context.ChatMessages.Add(userMessage);
        _context.ChatMessages.Add(assistantMessage);

        conversation.LastMessageAt = now;
        if (string.IsNullOrWhiteSpace(conversation.Title))
        {
            conversation.Title = request.Message.Trim().Length > 120
                ? request.Message.Trim()[..120]
                : request.Message.Trim();
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new SkillGapChatResponseDto
        {
            ConversationId = conversation.Id,
            AssistantMessage = fullAssistantContent,
            Suggestions = parsedResponse.Suggestions
        };
    }

    private async Task<ChatConversation> GetOrCreateConversationAsync(
        Guid? conversationId,
        Guid enterpriseId,
        Guid userId,
        int departmentId,
        CancellationToken cancellationToken)
    {
        if (conversationId.HasValue)
        {
            var existingConversation = await _context.ChatConversations
                .FirstOrDefaultAsync(
                    c => c.Id == conversationId.Value &&
                         c.EnterpriseId == enterpriseId &&
                         c.UserId == userId &&
                         !c.IsDeleted,
                    cancellationToken);

            if (existingConversation == null)
            {
                throw new Exception("Không tìm thấy cuộc trò chuyện.");
            }

            return existingConversation;
        }

        var conversation = new ChatConversation
        {
            Id = Guid.CreateVersion7(),
            EnterpriseId = enterpriseId,
            UserId = userId,
            DepartmentId = departmentId,
            Title = null,
            LastMessageAt = DateTime.UtcNow,
            IsDeleted = false
        };

        _context.ChatConversations.Add(conversation);
        return conversation;
    }

    private async Task<string> SendCompletionAsync(
        string systemPrompt,
        IReadOnlyList<ChatMessage> historyMessages,
        string userMessage,
        CancellationToken cancellationToken)
    {
        var messages = new List<object>
        {
            new { role = "system", content = systemPrompt }
        };

        foreach (var message in historyMessages)
        {
            var normalizedRole = string.Equals(message.Role, "assistant", StringComparison.OrdinalIgnoreCase)
                ? "assistant"
                : "user";

            messages.Add(new
            {
                role = normalizedRole,
                content = message.Content
            });
        }

        messages.Add(new
        {
            role = "user",
            content = userMessage.Trim()
        });

        var requestBody = new
        {
            model = _modelSettings.TrainingSuggestion.Trim(),
            messages,
            temperature = 0.2,
            response_format = new { type = "json_object" }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, CompletionsEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _groqSettings.ApiKey);
        request.Content = JsonContent.Create(requestBody);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Lỗi khi gọi Groq chat completion. Status: {Status}. Body: {Body}",
                response.StatusCode,
                responseContent);
            throw new HttpRequestException($"Groq chat completion failed: {response.StatusCode}");
        }

        var parsed = JsonSerializer.Deserialize<GroqChatResponse>(responseContent);
        return parsed?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
    }

    private static SkillGapChatResponseDto ParseAssistantResponse(string rawContent)
    {
        if (string.IsNullOrWhiteSpace(rawContent))
        {
            return BuildFallbackResponse();
        }

        var normalized = StripMarkdownCodeFences(rawContent);

        try
        {
            var payload = JsonSerializer.Deserialize<SkillGapAssistantPayload>(
                normalized,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (payload == null || string.IsNullOrWhiteSpace(payload.AssistantMessage))
            {
                return BuildFallbackResponse();
            }

            return new SkillGapChatResponseDto
            {
                AssistantMessage = payload.AssistantMessage.Trim(),
                Suggestions = payload.Suggestions?
                    .Where(s => !string.IsNullOrWhiteSpace(s.Title))
                    .Select(s => new SkillGapChatSuggestionDto
                    {
                        Title = s.Title.Trim(),
                        Description = s.Description?.Trim(),
                        CourseId = s.CourseId?.Trim(),
                        CourseName = s.CourseName?.Trim(),
                        SkillName = s.SkillName?.Trim()
                    })
                    .ToList() ?? []
            };
        }
        catch
        {
            return BuildFallbackResponse();
        }
    }

    private static SkillGapChatResponseDto BuildFallbackResponse()
    {
        return new SkillGapChatResponseDto
        {
            AssistantMessage = "Xin lỗi, tôi chưa thể phân tích dữ liệu lúc này. Vui lòng thử lại sau.",
            Suggestions = []
        };
    }

    private static string BuildSystemPrompt(
        string departmentName,
        IReadOnlyList<Employee> employees,
        IReadOnlyList<Course> courses)
    {
        var promptBuilder = new StringBuilder();

promptBuilder.AppendLine("Bạn là trợ lý phân tích năng lực nhân sự (skill gap advisor) cho doanh nghiệp.");
promptBuilder.AppendLine($"Phòng ban hiện tại: {departmentName}.");
promptBuilder.AppendLine("Bạn chỉ được trả lời bằng tiếng Việt.");
promptBuilder.AppendLine();

promptBuilder.AppendLine("=== QUY TẮC BẮT BUỘC ===");

promptBuilder.AppendLine("1. CHỈ phân tích những nhân viên CÓ skill gap. Nếu nhân viên đã đủ kỹ năng → KHÔNG đề cập.");
promptBuilder.AppendLine("2. Nếu CHỈ có 1-3 nhân viên có skill gap → phân tích RIÊNG từng người.");
promptBuilder.AppendLine("3. Nếu có NHIỀU nhân viên (>=4) có CÙNG skill gap → GOM NHÓM họ lại và phân tích chung.");
promptBuilder.AppendLine("4. Với mỗi cá nhân hoặc nhóm:");
promptBuilder.AppendLine("   - Xác định rõ: họ đang THIẾU kỹ năng gì");
promptBuilder.AppendLine("   - Giải thích NGẮN GỌN vì sao kỹ năng đó quan trọng cho công việc thực tế");
promptBuilder.AppendLine();

promptBuilder.AppendLine("5. Gợi ý khóa học phải tuân thủ:");
promptBuilder.AppendLine("   - CHỈ chọn từ danh sách khóa học được cung cấp (KHÔNG tự bịa)");
promptBuilder.AppendLine("   - Khóa học PHẢI dạy đúng skill còn thiếu");
promptBuilder.AppendLine("   - Sắp xếp theo lộ trình từ CƠ BẢN → NÂNG CAO (nếu cần)");
promptBuilder.AppendLine("   - Không chỉ đưa 1 khóa học chung chung, mà phải thể hiện progression (ví dụ: React cơ bản → React nâng cao)");
promptBuilder.AppendLine("   - Tối đa 2-3 khóa học cho mỗi cá nhân hoặc mỗi nhóm");
promptBuilder.AppendLine();

promptBuilder.AppendLine("6. KHÔNG được viết kiểu chung chung như:");
promptBuilder.AppendLine("   'khóa học giúp cải thiện kỹ năng X'");
promptBuilder.AppendLine("   → phải nói rõ kỹ năng đó dùng vào công việc gì");

promptBuilder.AppendLine("7. assistantMessage phải rõ ràng, dễ đọc, dùng \\n để xuống dòng.");

promptBuilder.AppendLine("8. suggestions:");
promptBuilder.AppendLine("   - Chỉ chứa TOP 3-5 khóa học QUAN TRỌNG NHẤT toàn bộ phòng ban");
promptBuilder.AppendLine("   - Ưu tiên các skill gap phổ biến (xuất hiện nhiều nhân viên)");
promptBuilder.AppendLine();

promptBuilder.AppendLine("=== FORMAT assistantMessage ===");
promptBuilder.AppendLine("Phân tích skill gap phòng ban:\\n");

promptBuilder.AppendLine("- Nếu ít người (1-3):");
promptBuilder.AppendLine("▶ [Tên nhân viên] ([Vị trí]):\\n");
promptBuilder.AppendLine("- Hiện có: ...\\n");
promptBuilder.AppendLine("- Thiếu: ... (giải thích ngắn vì sao cần)\\n");
promptBuilder.AppendLine("→ Lộ trình học: [Khóa 1 → Khóa 2]\\n");

promptBuilder.AppendLine("- Nếu nhiều người cùng thiếu skill:");
promptBuilder.AppendLine("▶ Nhóm [Tên skill] ([Danh sách nhân viên]):\\n");
promptBuilder.AppendLine("- Thiếu: ...\\n");
promptBuilder.AppendLine("- Lý do: ...\\n");
promptBuilder.AppendLine("→ Lộ trình học: [Khóa 1 → Khóa 2]\\n");

promptBuilder.AppendLine();

promptBuilder.AppendLine("=== OUTPUT ===");
promptBuilder.AppendLine("Bắt buộc trả về JSON object với schema:");
promptBuilder.AppendLine("{\"assistantMessage\":\"string\",\"suggestions\":[{\"title\":\"string\",\"description\":\"string\",\"courseId\":\"GUID\",\"courseName\":\"string\",\"skillName\":\"string\"}]}");

promptBuilder.AppendLine();
promptBuilder.AppendLine($"Danh sách nhân viên của phòng ban ({employees.Count} người):");
        if (employees.Count == 0)
        {
            promptBuilder.AppendLine("- Chưa có dữ liệu nhân viên.");
        }
        else
        {
            foreach (var employee in employees)
            {
                var fullName = employee.User?.FullName ?? employee.EmployeeCode;
                var position = string.IsNullOrWhiteSpace(employee.Position)
                    ? "chưa rõ vị trí"
                    : employee.Position.Trim();
                var skillText = string.IsNullOrWhiteSpace(employee.SkillDescription)
                    ? "chưa có thông tin kỹ năng"
                    : employee.SkillDescription.Trim();
                promptBuilder.AppendLine($"- {fullName} | Vị trí: {position} | Kỹ năng: {skillText}");
            }
        }

        promptBuilder.AppendLine();
        promptBuilder.AppendLine($"Danh sách khóa học khả dụng ({courses.Count} khóa):");
        if (courses.Count == 0)
        {
            promptBuilder.AppendLine("- Chưa có khóa học.");
        }
        else
        {
            foreach (var course in courses)
            {
                var skillNames = course.CourseSkills
                    .Select(cs => cs.Skill?.SkillName)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Select(name => name!.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var skillsSummary = skillNames.Count > 0 ? string.Join(", ", skillNames) : "chưa gắn kỹ năng";
                promptBuilder.AppendLine($"- [ID: {course.Id}] {course.CourseName}: kỹ năng [{skillsSummary}]");
            }
        }

        return promptBuilder.ToString().Trim();
    }

    private static string StripMarkdownCodeFences(string content)
    {
        var trimmed = content.Trim();
        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            return trimmed;
        }

        var firstNewLine = trimmed.IndexOf('\n');
        if (firstNewLine >= 0)
        {
            trimmed = trimmed[(firstNewLine + 1)..];
        }

        var closingFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
        if (closingFence > 0)
        {
            trimmed = trimmed[..closingFence];
        }

        return trimmed.Trim();
    }

    private static string FormatAssistantContent(SkillGapChatResponseDto response)
    {
        return response.AssistantMessage;
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

    private sealed class SkillGapAssistantPayload
    {
        [JsonPropertyName("assistantMessage")]
        public string? AssistantMessage { get; set; }

        [JsonPropertyName("suggestions")]
        public List<SkillGapAssistantSuggestionPayload>? Suggestions { get; set; }
    }

    private sealed class SkillGapAssistantSuggestionPayload
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("courseId")]
        public string? CourseId { get; set; }

        [JsonPropertyName("courseName")]
        public string? CourseName { get; set; }

        [JsonPropertyName("skillName")]
        public string? SkillName { get; set; }
    }
}
