using ERMS.Application.Features.Quizzes.Commands.CreateQuizQuestion;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Training;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Quizzes.Commands.CreateQuizQuestion
{
    public class CreateQuizQuestionHandlerTests
    {
        private readonly Mock<IERMSDbContext> _contextMock;
        private readonly CreateQuizQuestionHandler _handler;

        public CreateQuizQuestionHandlerTests()
        {
            _contextMock = new Mock<IERMSDbContext>();
            _handler = new CreateQuizQuestionHandler(_contextMock.Object);
        }

        [Fact]
        public async Task Handle_QuizNotFound_ShouldThrowException()
        {
            _contextMock.Setup(x => x.Quizzes)
                .Returns(new List<Quiz>().AsQueryable().BuildMockDbSet().Object);

            var command = new CreateQuizQuestionCommand
            {
                QuizId = Guid.NewGuid(),
                QuestionText = "Test"
            };

            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Không tìm thấy bài kiểm tra");
        }

        [Fact]
        public async Task Handle_ValidRequest_ShouldCreateQuestion()
        {
            var quizId = Guid.NewGuid();

            var quizzes = new List<Quiz>
            {
                new Quiz { Id = quizId }
            };

            _contextMock.Setup(x => x.Quizzes)
                .Returns(quizzes.AsQueryable().BuildMockDbSet().Object);

            _contextMock.Setup(x => x.QuizQuestions.Add(It.IsAny<QuizQuestion>()));

            _contextMock.Setup(x =>
                x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var command = new CreateQuizQuestionCommand
            {
                QuizId = quizId,
                QuestionText = "Test question",
                Options = "{}",
                CorrectAnswer = "A"
            };

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Should().NotBeEmpty();
        }
    }
}