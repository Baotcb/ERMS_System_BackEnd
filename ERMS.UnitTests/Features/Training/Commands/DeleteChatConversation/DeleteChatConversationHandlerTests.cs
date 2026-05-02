using ERMS.Application.Features.Training.Commands.DeleteChatConversation;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Training;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Training.Commands.DeleteChatConversation;

public class DeleteChatConversationHandlerTests
{
    private readonly Mock<IERMSDbContext> _contextMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly DeleteChatConversationHandler _handler;

    public DeleteChatConversationHandlerTests()
    {
        _handler = new DeleteChatConversationHandler(_contextMock.Object, _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenConversationDoesNotBelongToCurrentUser()
    {
        var enterpriseId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
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

        Func<Task> act = () => _handler.Handle(new DeleteChatConversationCommand
        {
            ConversationId = Guid.NewGuid()
        }, CancellationToken.None);

        await act.Should()
            .ThrowAsync<Exception>()
            .WithMessage("*cuộc trò chuyện*");
    }

    [Fact]
    public async Task Handle_ShouldSoftDeleteConversation()
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

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);
        _currentUserServiceMock.Setup(x => x.Roles).Returns(["DepartmentHead"]);

        _contextMock.Setup(x => x.ChatConversations).Returns(new List<ChatConversation>
        {
            conversation
        }.AsQueryable().BuildMockDbSet().Object);
        _contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(new DeleteChatConversationCommand
        {
            ConversationId = conversationId
        }, CancellationToken.None);

        result.Should().BeTrue();
        conversation.IsDeleted.Should().BeTrue();
        conversation.DeletedAt.Should().NotBeNull();
    }
}
