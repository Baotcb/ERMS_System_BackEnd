using ERMS.Application.Features.Feedback.Commands.ReplyFeedback;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Training;
using ERMS.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Feedback.Commands.ReplyFeedback
{
    public class ReplyFeedbackCommandHandlerTest : IDisposable
    {
        private readonly ERMSDbContext _context;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly ReplyFeedbackCommandHandler _handler;

        public ReplyFeedbackCommandHandlerTest()
        {
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ERMSDbContext(options);
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _handler = new ReplyFeedbackCommandHandler(_context, _currentUserServiceMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldReplySuccessfully_WhenDataIsValid()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

            var feedback = new CourseFeedback { Id = 1, CourseId = Guid.NewGuid(), EmployeeId = Guid.NewGuid() };
            _context.CourseFeedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            var command = new ReplyFeedbackCommand
            {
                FeedbackId = 1,
                ReplyContent = "Cảm ơn bạn đã phản hồi!",
                ParentReplyId = null
            };

            // Act
            var resultId = await _handler.Handle(command, CancellationToken.None);

            // Assert
            resultId.Should().BeGreaterThan(0);
            var reply = await _context.CourseFeedbackReplies.FindAsync(resultId);
            reply.Should().NotBeNull();
            reply!.ReplyContent.Should().Be("Cảm ơn bạn đã phản hồi!");
            reply.ReplyBy.Should().Be(userId);
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenFeedbackNotFound()
        {
            // Arrange
            var command = new ReplyFeedbackCommand { FeedbackId = 99, ReplyContent = "Test" };

            // Act & Assert
            var act = () => _handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<Exception>().WithMessage("Không tìm thấy phản hồi khóa học");
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}