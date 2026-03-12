using ERMS.Application.Features.Quizzes.Commands.SubmitAnswer;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Training;
using FluentAssertions;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Quizzes.Commands.SubmitAnswer
{
    public class SubmitAnswerCommandHandlerTests
    {
        private readonly Mock<IERMSDbContext> _contextMock;
        private readonly SubmitAnswerCommandHandler _handler;

        public SubmitAnswerCommandHandlerTests()
        {
            _contextMock = new Mock<IERMSDbContext>();
            _handler = new SubmitAnswerCommandHandler(_contextMock.Object);
        }

        [Fact]
        public async Task Handle_ValidRequest_ShouldCreateQuizAnswer()
        {
            QuizAnswer? capturedAnswer = null;

            _contextMock.Setup(x => x.QuizAnswers.Add(It.IsAny<QuizAnswer>()))
                .Callback<QuizAnswer>(x => capturedAnswer = x);

            _contextMock.Setup(x =>
                x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var command = new SubmitAnswerCommand
            {
                AttemptId = Guid.NewGuid(),
                QuestionId = Guid.NewGuid(),
                SelectedAnswer = "A"
            };

            await _handler.Handle(command, CancellationToken.None);

            capturedAnswer.Should().NotBeNull();
            capturedAnswer!.QuizAttemptId.Should().Be(command.AttemptId);
            capturedAnswer.QuizQuestionId.Should().Be(command.QuestionId);
            capturedAnswer.SelectedAnswer.Should().Be("A");

            _contextMock.Verify(x =>
                x.QuizAnswers.Add(It.IsAny<QuizAnswer>()),
                Times.Once);

            _contextMock.Verify(x =>
                x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldSetAnsweredAt()
        {
            QuizAnswer? capturedAnswer = null;

            _contextMock.Setup(x => x.QuizAnswers.Add(It.IsAny<QuizAnswer>()))
                .Callback<QuizAnswer>(x => capturedAnswer = x);

            _contextMock.Setup(x =>
                x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var command = new SubmitAnswerCommand
            {
                AttemptId = Guid.NewGuid(),
                QuestionId = Guid.NewGuid(),
                SelectedAnswer = "B"
            };

            await _handler.Handle(command, CancellationToken.None);

            capturedAnswer.Should().NotBeNull();
            capturedAnswer!.AnsweredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }
    }
}