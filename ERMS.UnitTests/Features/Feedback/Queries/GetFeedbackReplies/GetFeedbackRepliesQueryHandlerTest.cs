using ERMS.Application.Features.Feedback.Queries.GetFeedbackReplies;
using ERMS.Infrastructure.Data;
using ERMS.Domain.Entities.Training;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ERMS.UnitTests.Features.Feedback.Queries.GetFeedbackReplies
{
    public class GetFeedbackRepliesQueryHandlerTest : IDisposable
    {
        private readonly ERMSDbContext _context;
        private readonly GetFeedbackRepliesQueryHandler _handler;

        public GetFeedbackRepliesQueryHandlerTest()
        {
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ERMSDbContext(options);
            _handler = new GetFeedbackRepliesQueryHandler(_context);
        }

        [Fact]
        public async Task Handle_ShouldReturnNestedReplies()
        {
            // Arrange
            var feedbackId = 1;
            _context.CourseFeedbacks.Add(new CourseFeedback { Id = feedbackId });

            var r1 = new CourseFeedbackReply { Id = 10, FeedbackId = feedbackId, ReplyContent = "Root", ParentReplyId = null };
            var r2 = new CourseFeedbackReply { Id = 11, FeedbackId = feedbackId, ReplyContent = "Child", ParentReplyId = 10 };

            _context.CourseFeedbackReplies.AddRange(r1, r2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _handler.Handle(new GetFeedbackRepliesQuery { FeedbackId = feedbackId }, CancellationToken.None);

            // Assert
            result.Should().HaveCount(1);
            result[0].Id.Should().Be(10);
            result[0].Children.Should().HaveCount(1);
            result[0].Children[0].Id.Should().Be(11);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}