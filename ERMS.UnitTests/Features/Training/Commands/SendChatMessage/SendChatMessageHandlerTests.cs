using ERMS.Application.Features.Training.Commands.SendChatMessage;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Training.Commands.SendChatMessage;

public class SendChatMessageHandlerTests
{
    private readonly Mock<ISkillGapChatService> _chatServiceMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly SendChatMessageHandler _handler;

    public SendChatMessageHandlerTests()
    {
        _handler = new SendChatMessageHandler(
            _chatServiceMock.Object,
            _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenUserIsNotDepartmentHead()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
        _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(Guid.NewGuid());
        _currentUserServiceMock.Setup(x => x.Roles).Returns(new[] { AppRoles.Employee });

        var command = new SendChatMessageCommand
        {
            Message = "Xin chao"
        };

        Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*DepartmentHead*");
    }

    [Fact]
    public async Task Handle_ShouldForwardMessageToChatService_WhenAuthorized()
    {
        var userId = Guid.NewGuid();
        var enterpriseId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);
        _currentUserServiceMock.Setup(x => x.Roles).Returns(new[] { AppRoles.DepartmentHead });

        _chatServiceMock
            .Setup(x => x.AskAsync(It.IsAny<SkillGapChatRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SkillGapChatResponseDto
            {
                ConversationId = conversationId,
                AssistantMessage = "Chao ban",
                Suggestions = []
            });

        var command = new SendChatMessageCommand
        {
            ConversationId = conversationId,
            Message = "  Can giup toi  "
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.ConversationId.Should().Be(conversationId);
        result.AssistantMessage.Should().Be("Chao ban");

        _chatServiceMock.Verify(x => x.AskAsync(
                It.Is<SkillGapChatRequestDto>(request =>
                    request.ConversationId == conversationId &&
                    request.Message == "Can giup toi"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
