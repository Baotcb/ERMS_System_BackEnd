using ERMS.Application.Features.Training.Queries.GetChatHistory;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Training;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Training.Queries.GetChatHistory;

public class GetChatHistoryHandlerTests
{
    private readonly Mock<IERMSDbContext> _contextMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly GetChatHistoryHandler _handler;

    public GetChatHistoryHandlerTests()
    {
        _handler = new GetChatHistoryHandler(_contextMock.Object, _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenConversationDoesNotBelongToCurrentUser()
    {
        var enterpriseId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
        _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

        _contextMock.Setup(x => x.ChatConversations).Returns(new List<ChatConversation>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                UserId = Guid.NewGuid(),
                IsDeleted = false
            }
        }.AsQueryable().BuildMockDbSet().Object);

        Func<Task> act = () => _handler.Handle(new GetChatHistoryQuery
        {
            ConversationId = Guid.NewGuid()
        }, CancellationToken.None);

        await act.Should()
            .ThrowAsync<Exception>()
            .WithMessage("*cuộc trò chuyện*");
    }

    [Fact]
    public async Task Handle_ShouldReturnMessagesOldestFirst_WithPaging()
    {
        var enterpriseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var conversation = new ChatConversation
        {
            Id = conversationId,
            EnterpriseId = enterpriseId,
            UserId = userId,
            IsDeleted = false
        };
        var messages = new List<ChatMessage>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ChatConversationId = conversationId,
                EnterpriseId = enterpriseId,
                UserId = userId,
                Role = "user",
                Content = "m1",
                CreatedAt = DateTime.UtcNow.AddMinutes(-3),
                IsDeleted = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                ChatConversationId = conversationId,
                EnterpriseId = enterpriseId,
                UserId = userId,
                Role = "assistant",
                Content = "m2",
                CreatedAt = DateTime.UtcNow.AddMinutes(-2),
                IsDeleted = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                ChatConversationId = conversationId,
                EnterpriseId = enterpriseId,
                UserId = userId,
                Role = "user",
                Content = "m3",
                CreatedAt = DateTime.UtcNow.AddMinutes(-1),
                IsDeleted = false
            }
        };

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

        _contextMock.Setup(x => x.ChatConversations).Returns(new List<ChatConversation> { conversation }.AsQueryable().BuildMockDbSet().Object);
        _contextMock.Setup(x => x.ChatMessages).Returns(messages.AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetChatHistoryQuery
        {
            ConversationId = conversationId,
            Page = 1,
            PageSize = 2
        }, CancellationToken.None);

        result.TotalCount.Should().Be(3);
        result.Items.Should().HaveCount(2);
        result.Items[0].Content.Should().Be("m1");
        result.Items[1].Content.Should().Be("m2");
    }
}
