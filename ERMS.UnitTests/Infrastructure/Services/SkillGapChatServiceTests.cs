using System.Net;
using System.Text;
using System.Text.Json;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Organization;
using ERMS.Domain.Entities.Skill;
using ERMS.Domain.Entities.Training;
using ERMS.Infrastructure.Configuration;
using ERMS.Infrastructure.Services;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace ERMS.UnitTests.Infrastructure.Services;

public class SkillGapChatServiceTests
{
    private readonly Mock<IERMSDbContext> _contextMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<ILogger<SkillGapChatService>> _loggerMock = new();

    private readonly Guid _enterpriseId = Guid.NewGuid();
    private readonly int _departmentId = 12;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _conversationId = Guid.NewGuid();

    private readonly GroqModelSettings _modelSettings = new()
    {
        TrainingSuggestion = "qwen-training-suggestion"
    };

    [Fact]
    public async Task AskAsync_ShouldSendVietnameseJsonPrompt_AndUseTrainingSuggestionModel()
    {
        var requestCapture = new List<string>();
        var service = CreateService(
            new CapturingHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{"choices":[{"message":{"content":"{\"assistantMessage\":\"Chào bạn\",\"suggestions\":[]}"}}]}""",
                        Encoding.UTF8,
                        "application/json")
                },
                requestCapture),
            out var addedConversations,
            out var addedMessages);

        SetupEnterpriseData();
        SetupConversationHistory(addedConversations, addedMessages);

        var result = await service.AskAsync(new SkillGapChatRequestDto
        {
            Message = "Tôi cần gợi ý đào tạo cho team backend",
            ConversationId = _conversationId
        }, CancellationToken.None);

        result.AssistantMessage.Should().Be("Chào bạn");
        result.Suggestions.Should().BeEmpty();
        addedConversations.Should().BeEmpty();
        addedMessages.Should().HaveCount(2);

        requestCapture.Should().ContainSingle();
        var requestJson = JsonDocument.Parse(requestCapture[0]);
        requestJson.RootElement.GetProperty("model").GetString().Should().Be("qwen-training-suggestion");
        requestJson.RootElement.GetProperty("response_format").GetProperty("type").GetString().Should().Be("json_object");

        var systemMessage = requestJson.RootElement.GetProperty("messages")[0].GetProperty("content").GetString();
        systemMessage.Should().Contain("assistantMessage");
        systemMessage.Should().Contain("suggestions");
        systemMessage.Should().Contain("tiếng Việt");
        systemMessage.Should().Contain("20 tin nhắn gần nhất");
        systemMessage.Should().Contain("nhân viên của phòng ban");
    }

    [Fact]
    public async Task AskAsync_ShouldPersistConversationAndMessages()
    {
        var service = CreateService(
            new CapturingHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{"choices":[{"message":{"content":"{\"assistantMessage\":\"Bạn nên học thêm C#\",\"suggestions\":[{\"title\":\"C# nâng cao\",\"description\":\"Tập trung vào generic và async/await\"}]}"}}]}""",
                        Encoding.UTF8,
                        "application/json")
                }),
            out var addedConversations,
            out var addedMessages);

        SetupEmptyConversationState(addedConversations, addedMessages);
        SetupEnterpriseData();

        var result = await service.AskAsync(new SkillGapChatRequestDto
        {
            Message = "Gợi ý lộ trình học cho team backend"
        }, CancellationToken.None);

        result.AssistantMessage.Should().Be("Bạn nên học thêm C#");
        result.Suggestions.Should().ContainSingle();
        result.Suggestions[0].Title.Should().Be("C# nâng cao");

        addedConversations.Should().ContainSingle();
        addedConversations[0].EnterpriseId.Should().Be(_enterpriseId);
        addedConversations[0].UserId.Should().Be(_userId);

        addedMessages.Should().HaveCount(2);
        addedMessages[0].Role.Should().Be("user");
        addedMessages[0].UserId.Should().Be(_userId);
        addedMessages[0].Content.Should().Be("Gợi ý lộ trình học cho team backend");
        addedMessages[1].Role.Should().Be("assistant");
        addedMessages[1].UserId.Should().Be(_userId);
        addedMessages[1].Content.Should().Contain("Bạn nên học thêm C#");
    }

    [Fact]
    public async Task AskAsync_ShouldFallback_WhenGroqReturnsMalformedJson()
    {
        var service = CreateService(
            new CapturingHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{"choices":[{"message":{"content":"khong-phai-json"}}]}""",
                        Encoding.UTF8,
                        "application/json")
                }),
            out var addedConversations,
            out var addedMessages);

        SetupEmptyConversationState(addedConversations, addedMessages);
        SetupEnterpriseData();

        var result = await service.AskAsync(new SkillGapChatRequestDto
        {
            Message = "Đề xuất nội dung đào tạo"
        }, CancellationToken.None);

        result.AssistantMessage.Should().Contain("chưa thể");
        result.Suggestions.Should().BeEmpty();

        addedConversations.Should().ContainSingle();
        addedMessages.Should().HaveCount(2);
        addedMessages[1].Role.Should().Be("assistant");
        addedMessages[1].Content.Should().Contain("chưa thể");
    }

    private SkillGapChatService CreateService(
        HttpMessageHandler handler,
        out List<ChatConversation> addedConversations,
        out List<ChatMessage> addedMessages)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.groq.com/openai/v1/")
        };

        addedConversations = [];
        addedMessages = [];
        _contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(_enterpriseId);
        _currentUserServiceMock.Setup(x => x.GetDepartmentIdAsync()).ReturnsAsync(_departmentId);

        return new SkillGapChatService(
            httpClient,
            _loggerMock.Object,
            Options.Create(new GroqSettings { ApiKey = "test-key" }),
            Options.Create(_modelSettings),
            _contextMock.Object,
            _currentUserServiceMock.Object);
    }

    private void SetupEmptyConversationState(
        List<ChatConversation> addedConversations,
        List<ChatMessage> addedMessages)
    {
        var conversationsDbSet = new List<ChatConversation>().AsQueryable().BuildMockDbSet();
        conversationsDbSet.Setup(d => d.Add(It.IsAny<ChatConversation>()))
            .Callback<ChatConversation>(entity => addedConversations.Add(entity));

        var messagesDbSet = new List<ChatMessage>().AsQueryable().BuildMockDbSet();
        messagesDbSet.Setup(d => d.Add(It.IsAny<ChatMessage>()))
            .Callback<ChatMessage>(entity => addedMessages.Add(entity));

        _contextMock.Setup(x => x.ChatConversations).Returns(conversationsDbSet.Object);
        _contextMock.Setup(x => x.ChatMessages).Returns(messagesDbSet.Object);
    }

    private void SetupEnterpriseData()
    {
        var employees = new List<Employee>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseId = _enterpriseId,
                DepartmentId = _departmentId,
                IsDeleted = false,
                EmployeeCode = "E001",
                User = new ERMS.Domain.Entities.Identity.User
                {
                    FullName = "Nguyen Van A"
                }
            }
        };

        var course = new Course
        {
            Id = Guid.NewGuid(),
            EnterpriseId = _enterpriseId,
            CourseCode = "C-101",
            CourseName = "C# nâng cao",
            TrainerEmail = "trainer@erms.local",
            StartTime = DateTime.UtcNow.AddDays(1),
            IsDeleted = false,
            CourseSkills =
            [
                new CourseSkill
                {
                    Id = Guid.NewGuid(),
                    SkillId = Guid.NewGuid(),
                    SkillLevelGained = 3,
                    Skill = new Skill
                    {
                        Id = Guid.NewGuid(),
                        SkillName = "C#"
                    }
                }
            ]
        };

        _contextMock.Setup(x => x.Employees)
            .Returns(employees.AsQueryable().BuildMockDbSet().Object);
        _contextMock.Setup(x => x.Courses)
            .Returns(new List<Course> { course }.AsQueryable().BuildMockDbSet().Object);
        _contextMock.Setup(x => x.CourseSkills)
            .Returns(course.CourseSkills.AsQueryable().BuildMockDbSet().Object);
        _contextMock.Setup(x => x.Skills)
            .Returns(course.CourseSkills.Select(x => x.Skill).AsQueryable().BuildMockDbSet().Object);
    }

    private void SetupConversationHistory(
        List<ChatConversation> addedConversations,
        List<ChatMessage> addedMessages)
    {
        var conversation = new ChatConversation
        {
            Id = _conversationId,
            EnterpriseId = _enterpriseId,
            UserId = _userId,
            DepartmentId = _departmentId,
            IsDeleted = false,
            Messages = []
        };

        var oldMessages = new List<ChatMessage>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ChatConversationId = _conversationId,
                EnterpriseId = _enterpriseId,
                UserId = _userId,
                Role = "user",
                Content = "Xin chào",
                CreatedAt = DateTime.UtcNow.AddMinutes(-25),
                IsDeleted = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                ChatConversationId = _conversationId,
                EnterpriseId = _enterpriseId,
                UserId = _userId,
                Role = "assistant",
                Content = "Chào bạn",
                CreatedAt = DateTime.UtcNow.AddMinutes(-24),
                IsDeleted = false
            }
        };

        var conversationsDbSet = new List<ChatConversation> { conversation }.AsQueryable().BuildMockDbSet();
        conversationsDbSet.Setup(d => d.Add(It.IsAny<ChatConversation>()))
            .Callback<ChatConversation>(entity => addedConversations.Add(entity));

        var messagesDbSet = oldMessages.AsQueryable().BuildMockDbSet();
        messagesDbSet.Setup(d => d.Add(It.IsAny<ChatMessage>()))
            .Callback<ChatMessage>(entity => addedMessages.Add(entity));

        _contextMock.Setup(x => x.ChatConversations).Returns(conversationsDbSet.Object);
        _contextMock.Setup(x => x.ChatMessages).Returns(messagesDbSet.Object);
    }

    private sealed class CapturingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;
        private readonly List<string> _requestBodies;

        public CapturingHttpMessageHandler(
            Func<HttpRequestMessage, HttpResponseMessage> responseFactory,
            List<string>? requestBodies = null)
        {
            _responseFactory = responseFactory;
            _requestBodies = requestBodies ?? [];
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            _requestBodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            return _responseFactory(request);
        }
    }
}
