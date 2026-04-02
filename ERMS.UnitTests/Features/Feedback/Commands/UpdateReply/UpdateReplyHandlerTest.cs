using ERMS.Application.Features.Feedback.Commands.UpdateReply;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Training;
using ERMS.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ERMS.UnitTests.Features.Feedback.Commands.UpdateReply
{
    public class UpdateReplyHandlerTest : IDisposable
    {
        private readonly ERMSDbContext _context;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly UpdateReplyHandler _handler;

        public UpdateReplyHandlerTest()
        {
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ERMSDbContext(options);
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _handler = new UpdateReplyHandler(_context, _currentUserServiceMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldUpdate_WhenUserIsOwner()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

            var reply = new CourseFeedbackReply { Id = 1, ReplyBy = userId, ReplyContent = "Old Content", FeedbackId = 1 };
            _context.CourseFeedbackReplies.Add(reply);
            await _context.SaveChangesAsync();

            var command = new UpdateReplyCommand { ReplyId = 1, Content = "Updated Content" };

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            var updated = await _context.CourseFeedbackReplies.FindAsync(1);
            updated!.ReplyContent.Should().Be("Updated Content");
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorized_WhenUserIsNotOwner()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid()); // User khác
            var reply = new CourseFeedbackReply { Id = 1, ReplyBy = Guid.NewGuid(), ReplyContent = "Content" };
            _context.CourseFeedbackReplies.Add(reply);
            await _context.SaveChangesAsync();

            var command = new UpdateReplyCommand { ReplyId = 1, Content = "Hack" };

            // Act & Assert
            var act = () => _handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}