using ERMS.Application.Features.Feedback.Commands.DeleteReply;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Training;
using ERMS.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ERMS.UnitTests.Features.Feedback.Commands.DeleteReply
{
    public class DeleteReplyCommandHandlerTest : IDisposable
    {
        private readonly ERMSDbContext _context;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly DeleteReplyCommandHandler _handler;

        public DeleteReplyCommandHandlerTest()
        {
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ERMSDbContext(options);
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _handler = new DeleteReplyCommandHandler(_context, _currentUserServiceMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldSoftDeleteReplyAndChildren()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

            var parent = new CourseFeedbackReply { Id = 1, ReplyBy = userId, IsDeleted = false, ReplyContent = "content1" };
            var child = new CourseFeedbackReply { Id = 2, ParentReplyId = 1, IsDeleted = false, ReplyContent = "content2" };

            _context.CourseFeedbackReplies.AddRange(parent, child);
            await _context.SaveChangesAsync();

            // Act
            await _handler.Handle(new DeleteReplyCommand { ReplyId = 1 }, CancellationToken.None);

            // Assert
            parent.IsDeleted.Should().BeTrue();
            var childInDb = await _context.CourseFeedbackReplies.FindAsync(2);
            childInDb!.IsDeleted.Should().BeTrue();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}