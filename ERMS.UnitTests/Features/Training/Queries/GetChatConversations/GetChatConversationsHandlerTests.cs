using ERMS.Application.Features.Training.Queries.GetChatConversations;
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

namespace ERMS.UnitTests.Features.Training.Queries.GetChatConversations;

public class GetChatConversationsHandlerTests
{
    private readonly Mock<IERMSDbContext> _contextMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly GetChatConversationsHandler _handler;

    public GetChatConversationsHandlerTests()
    {
        _handler = new GetChatConversationsHandler(_contextMock.Object, _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenEnterpriseMissing()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
        _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync((Guid?)null);

        Func<Task> act = () => _handler.Handle(new GetChatConversationsQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_ShouldReturnCurrentUsersConversations_NewestFirst()
    {
        var enterpriseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var older = new ChatConversation
        {
            Id = Guid.NewGuid(),
            EnterpriseId = enterpriseId,
            UserId = userId,
            Title = "Older",
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            LastMessageAt = DateTime.UtcNow.AddDays(-1),
            IsDeleted = false
        };
        var newer = new ChatConversation
        {
            Id = Guid.NewGuid(),
            EnterpriseId = enterpriseId,
            UserId = userId,
            Title = "Newer",
            CreatedAt = DateTime.UtcNow.AddDays(-3),
            LastMessageAt = DateTime.UtcNow,
            IsDeleted = false
        };
        var deleted = new ChatConversation
        {
            Id = Guid.NewGuid(),
            EnterpriseId = enterpriseId,
            UserId = userId,
            Title = "Deleted",
            CreatedAt = DateTime.UtcNow,
            LastMessageAt = DateTime.UtcNow,
            IsDeleted = true
        };
        var foreignEnterprise = new ChatConversation
        {
            Id = Guid.NewGuid(),
            EnterpriseId = Guid.NewGuid(),
            UserId = userId,
            Title = "Foreign",
            CreatedAt = DateTime.UtcNow,
            LastMessageAt = DateTime.UtcNow,
            IsDeleted = false
        };

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

        _contextMock.Setup(x => x.ChatConversations).Returns(new List<ChatConversation>
        {
            older,
            newer,
            deleted,
            foreignEnterprise
        }.AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetChatConversationsQuery
        {
            Page = 1,
            PageSize = 10
        }, CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items[0].Id.Should().Be(newer.Id);
        result.Items[1].Id.Should().Be(older.Id);
    }
}
