using ERMS.Application.Features.Quizzes.Commands.SubmitAnswer;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Training;
using ERMS.Infrastructure.Data; // Đảm bảo using đúng namespace của DbContext
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Quizzes.Commands.SubmitAnswer
{
    public class SubmitAnswerHandlerTests : IDisposable
    {
        private readonly ERMSDbContext _context;
        private readonly SubmitAnswerHandler _handler;

        public SubmitAnswerHandlerTests()
        {
            // 1. Khởi tạo InMemoryDatabase
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ERMSDbContext(options);

            // 2. Khởi tạo Handler với Context thật (nhưng data ảo)
            _handler = new SubmitAnswerHandler(_context);
        }

        [Fact]
        public async Task Handle_ValidRequest_ShouldCreateQuizAnswer()
        {
            // Arrange
            var attemptId = Guid.NewGuid();
            var questionId = Guid.NewGuid();

            // Nếu Handler của bạn kiểm tra Attempt có tồn tại không, hãy thêm nó vào đây
            _context.QuizAttempts.Add(new QuizAttempt { Id = attemptId });
            await _context.SaveChangesAsync();

            var command = new SubmitAnswerCommand
            {
                AttemptId = attemptId,
                QuestionId = questionId,
                SelectedAnswer = "A"
            };

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            var answerInDb = await _context.QuizAnswers
                .FirstOrDefaultAsync(x => x.QuizAttemptId == attemptId && x.QuizQuestionId == questionId);

            answerInDb.Should().NotBeNull();
            answerInDb!.SelectedAnswer.Should().Be("A");
            answerInDb.AnsweredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task Handle_ShouldUpdate_WhenAnswerAlreadyExists()
        {
            // Arrange: Đã có câu trả lời cũ trong DB
            var attemptId = Guid.NewGuid();
            var questionId = Guid.NewGuid();

            var existingAnswer = new QuizAnswer
            {
                QuizAttemptId = attemptId,
                QuizQuestionId = questionId,
                SelectedAnswer = "A",
                AnsweredAt = DateTime.UtcNow.AddMinutes(-10)
            };

            _context.QuizAnswers.Add(existingAnswer);
            await _context.SaveChangesAsync();

            var command = new SubmitAnswerCommand
            {
                AttemptId = attemptId,
                QuestionId = questionId,
                SelectedAnswer = "B" // Cập nhật từ A sang B
            };

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            var answerInDb = await _context.QuizAnswers.CountAsync();
            answerInDb.Should().Be(1); // Không tạo mới, chỉ cập nhật

            var updatedAnswer = await _context.QuizAnswers.FirstAsync();
            updatedAnswer.SelectedAnswer.Should().Be("B");
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}