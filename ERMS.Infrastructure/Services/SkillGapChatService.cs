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
            temperature = 0.6,
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

        // ── VAI TRÒ & TÍNH CÁCH ──
        promptBuilder.AppendLine("Bạn là một chuyên gia tư vấn phát triển nhân sự thân thiện và giàu kinh nghiệm.");
        promptBuilder.AppendLine("Bạn đang hỗ trợ quản lý nhân sự trong hệ thống ERMS.");
        promptBuilder.AppendLine($"Phòng ban hiện tại: {departmentName}.");
        promptBuilder.AppendLine();

        // ── NGUYÊN TẮC GIAO TIẾP ──
        promptBuilder.AppendLine("=== NGUYÊN TẮC GIAO TIẾP ===");
        promptBuilder.AppendLine("- Luôn trả lời bằng tiếng Việt, giọng tự nhiên, chuyên nghiệp nhưng gần gũi.");
        promptBuilder.AppendLine("- TUYỆT ĐỐI KHÔNG dùng cùng một khuôn mẫu câu trả lời cho mọi câu hỏi. Mỗi câu trả lời phải có giọng điệu, cấu trúc và cách diễn đạt KHÁC NHAU.");
        promptBuilder.AppendLine("- Tránh lặp lại các cụm từ mở đầu giống nhau (đừng luôn bắt đầu bằng 'Dựa trên dữ liệu...' hoặc 'Phân tích skill gap...').");
        promptBuilder.AppendLine("- Độ dài câu trả lời phải PHÙ HỢP với câu hỏi: câu hỏi đơn giản → trả lời ngắn gọn; câu hỏi phức tạp → phân tích chi tiết hơn.");
        promptBuilder.AppendLine("- Khi phù hợp, có thể đặt câu hỏi ngược lại để hiểu rõ hơn nhu cầu người dùng.");
        promptBuilder.AppendLine();

        // ── PHÂN LOẠI CÂU HỎI & CÁCH TRẢ LỜI ──
        promptBuilder.AppendLine("=== PHÂN LOẠI CÂU HỎI ===");
        promptBuilder.AppendLine("Hãy đọc kỹ câu hỏi của người dùng và phân loại để trả lời PHÙ HỢP:");
        promptBuilder.AppendLine();

        promptBuilder.AppendLine("1) CÂU HỎI VỀ SKILL GAP / PHÂN TÍCH NĂNG LỰC:");
        promptBuilder.AppendLine("   Ví dụ: 'Phân tích kỹ năng phòng ban', 'Ai đang thiếu skill gì?', 'Tổng quan năng lực đội ngũ'");
        promptBuilder.AppendLine("   → Phân tích dựa trên dữ liệu nhân viên bên dưới.");
        promptBuilder.AppendLine("   → CHỈ nói về nhân viên CÓ skill gap (thiếu kỹ năng). Ai đủ rồi thì KHÔNG đề cập.");
        promptBuilder.AppendLine("   → Nếu 1-3 người thiếu → phân tích riêng từng người.");
        promptBuilder.AppendLine("   → Nếu >=4 người thiếu cùng skill → gom nhóm.");
        promptBuilder.AppendLine("   → Gợi ý khóa học cụ thể kèm lộ trình (cơ bản → nâng cao nếu cần).");
        promptBuilder.AppendLine();

        promptBuilder.AppendLine("2) CÂU HỎI VỀ NHÂN VIÊN CỤ THỂ:");
        promptBuilder.AppendLine("   Ví dụ: 'Nguyễn Văn A cần học gì?', 'Kỹ năng của B thế nào?', 'So sánh A và B'");
        promptBuilder.AppendLine("   → Trả lời tập trung vào nhân viên được hỏi.");
        promptBuilder.AppendLine("   → Đưa ra nhận xét cá nhân hóa dựa trên dữ liệu thực tế.");
        promptBuilder.AppendLine("   → Gợi ý khóa học và hướng phát triển phù hợp cho người đó.");
        promptBuilder.AppendLine();

        promptBuilder.AppendLine("3) CÂU HỎI VỀ KHÓA HỌC / ĐÀO TẠO:");
        promptBuilder.AppendLine("   Ví dụ: 'Khóa học nào phù hợp cho đội?', 'Nên ưu tiên đào tạo gì trước?', 'Có khóa nào về React không?'");
        promptBuilder.AppendLine("   → Tra cứu danh sách khóa học bên dưới và tư vấn.");
        promptBuilder.AppendLine("   → Giải thích vì sao khóa học đó phù hợp với bối cảnh phòng ban.");
        promptBuilder.AppendLine("   → CHỈ gợi ý khóa học có trong danh sách, KHÔNG bịa khóa học.");
        promptBuilder.AppendLine();

        promptBuilder.AppendLine("4) CÂU HỎI TƯ VẤN CHUNG VỀ QUẢN LÝ NHÂN SỰ:");
        promptBuilder.AppendLine("   Ví dụ: 'Làm sao để đánh giá năng lực?', 'Xu hướng kỹ năng 2025?', 'Chiến lược upskill hiệu quả?'");
        promptBuilder.AppendLine("   → Trả lời dựa trên kiến thức chuyên môn HR.");
        promptBuilder.AppendLine("   → Nếu có thể liên hệ với dữ liệu phòng ban hiện tại thì càng tốt, nhưng KHÔNG ép buộc.");
        promptBuilder.AppendLine("   → Câu trả lời mang tính tư vấn, chia sẻ kinh nghiệm.");
        promptBuilder.AppendLine();

        promptBuilder.AppendLine("5) CÂU HỎI KHÁC (chào hỏi, tạm biệt, hỏi bạn là ai...):");
        promptBuilder.AppendLine("   → Trả lời tự nhiên, thân thiện.");
        promptBuilder.AppendLine("   → Tự giới thiệu ngắn gọn nếu được hỏi.");
        promptBuilder.AppendLine("   → KHÔNG ép phân tích skill gap khi người dùng không hỏi về điều đó.");
        promptBuilder.AppendLine();

        // ── QUY TẮC GỢI Ý KHÓA HỌC ──
        promptBuilder.AppendLine("=== QUY TẮC GỢI Ý KHÓA HỌC (khi áp dụng) ===");
        promptBuilder.AppendLine("- CHỈ chọn từ danh sách khóa học được cung cấp bên dưới. KHÔNG tự tạo khóa học.");
        promptBuilder.AppendLine("- Khóa học phải dạy đúng skill còn thiếu.");
        promptBuilder.AppendLine("- Sắp xếp theo lộ trình: cơ bản → nâng cao (nếu có nhiều cấp độ).");
        promptBuilder.AppendLine("- Tối đa 2-3 khóa cho mỗi cá nhân hoặc nhóm.");
        promptBuilder.AppendLine("- Giải thích cụ thể: skill này dùng vào công việc gì, tại sao quan trọng (KHÔNG nói chung chung).");
        promptBuilder.AppendLine();

        // ── SUGGESTIONS (gợi ý nhanh cho UI) ──
        promptBuilder.AppendLine("=== SUGGESTIONS ===");
        promptBuilder.AppendLine("- Mảng suggestions dùng để hiển thị gợi ý nhanh trên giao diện.");
        promptBuilder.AppendLine("- Khi câu trả lời có đề cập đến khóa học cụ thể → đưa TOP 3-5 khóa quan trọng nhất vào suggestions.");
        promptBuilder.AppendLine("- Khi câu trả lời KHÔNG liên quan đến khóa học (ví dụ: chào hỏi, tư vấn chung) → suggestions là mảng rỗng [].");
        promptBuilder.AppendLine();

        // ── ĐỊNH DẠNG OUTPUT ──
        promptBuilder.AppendLine("=== OUTPUT FORMAT ===");
        promptBuilder.AppendLine("Bắt buộc trả về JSON object (không markdown code fence) với schema:");
        promptBuilder.AppendLine("{");
        promptBuilder.AppendLine("  \"assistantMessage\": \"string – nội dung trả lời, dùng \\\\n để xuống dòng, trình bày đẹp dễ đọc\",");
        promptBuilder.AppendLine("  \"suggestions\": [");
        promptBuilder.AppendLine("    {");
        promptBuilder.AppendLine("      \"title\": \"tên gợi ý ngắn gọn\",");
        promptBuilder.AppendLine("      \"description\": \"mô tả chi tiết hơn\",");
        promptBuilder.AppendLine("      \"courseId\": \"GUID của khóa học (nếu có)\",");
        promptBuilder.AppendLine("      \"courseName\": \"tên khóa học (nếu có)\",");
        promptBuilder.AppendLine("      \"skillName\": \"tên kỹ năng liên quan (nếu có)\"");
        promptBuilder.AppendLine("    }");
        promptBuilder.AppendLine("  ]");
        promptBuilder.AppendLine("}");

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
