using ERMS.Application.Features.Quizzes.Commands.SubmitQuiz;
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

namespace ERMS.UnitTests.Features.Quizzes.Commands.SubmitQuiz
{
    public class SubmitQuizCommandHandlerTests
    {
        private readonly Mock<IERMSDbContext> _contextMock;
        private readonly SubmitQuizCommandHandler _handler;

        public SubmitQuizCommandHandlerTests()
        {
            _contextMock = new Mock<IERMSDbContext>();
            _handler = new SubmitQuizCommandHandler(_contextMock.Object);
        }

        private void SetupQuizAttempts(List<QuizAttempt> attempts)
        {
            var dbSet = attempts.AsQueryable().BuildMockDbSet();

            _contextMock.Setup(x => x.QuizAttempts)
                .Returns(dbSet.Object);
        }

        [Fact]
        public async Task Handle_AttemptNotFound_ShouldThrowException()
        {
            SetupQuizAttempts(new List<QuizAttempt>());

            var command = new SubmitQuizCommand
            {
                AttemptId = Guid.NewGuid()
            };

            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            await act.Should()
                .ThrowAsync<Exception>()
                .WithMessage("Không tìm thấy lượt làm bài");
        }

        [Fact]
        public async Task Handle_AllAnswersCorrect_ShouldReturnFullScore()
        {
            var attemptId = Guid.NewGuid();

            var question1 = new QuizQuestion
            {
                Id = Guid.NewGuid(),
                CorrectAnswer = "A",
                Points = 1
            };

            var question2 = new QuizQuestion
            {
                Id = Guid.NewGuid(),
                CorrectAnswer = "B",
                Points = 1
            };

            var attempt = new QuizAttempt
            {
                Id = attemptId,
                TotalQuestions = 2,
                Quiz = new Quiz
                {
                    PassingScore = 50,
                    Questions = new List<QuizQuestion> { question1, question2 }
                },
                QuizAnswers = new List<QuizAnswer>
                {
                    new QuizAnswer
                    {
                        QuizQuestionId = question1.Id,
                        SelectedAnswer = "A"
                    },
                    new QuizAnswer
                    {
                        QuizQuestionId = question2.Id,
                        SelectedAnswer = "B"
                    }
                }
            };

            SetupQuizAttempts(new List<QuizAttempt> { attempt });

            _contextMock.Setup(x =>
                x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var command = new SubmitQuizCommand
            {
                AttemptId = attemptId
            };

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Score.Should().Be(100);
            result.CorrectAnswers.Should().Be(2);
            result.TotalQuestions.Should().Be(2);
            result.IsPassed.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_PartialCorrect_ShouldCalculateScoreCorrectly()
        {
            var attemptId = Guid.NewGuid();

            var question1 = new QuizQuestion
            {
                Id = Guid.NewGuid(),
                CorrectAnswer = "A",
                Points = 1
            };

            var question2 = new QuizQuestion
            {
                Id = Guid.NewGuid(),
                CorrectAnswer = "B",
                Points = 1
            };

            var attempt = new QuizAttempt
            {
                Id = attemptId,
                TotalQuestions = 2,
                Quiz = new Quiz
                {
                    PassingScore = 70,
                    Questions = new List<QuizQuestion> { question1, question2 }
                },
                QuizAnswers = new List<QuizAnswer>
                {
                    new QuizAnswer
                    {
                        QuizQuestionId = question1.Id,
                        SelectedAnswer = "A"
                    },
                    new QuizAnswer
                    {
                        QuizQuestionId = question2.Id,
                        SelectedAnswer = "C"
                    }
                }
            };

            SetupQuizAttempts(new List<QuizAttempt> { attempt });

            _contextMock.Setup(x =>
                x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var command = new SubmitQuizCommand
            {
                AttemptId = attemptId
            };

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Score.Should().Be(50);
            result.CorrectAnswers.Should().Be(1);
            result.IsPassed.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_ShouldUpdateAttemptAndSaveChanges()
        {
            var attemptId = Guid.NewGuid();

            var question = new QuizQuestion
            {
                Id = Guid.NewGuid(),
                CorrectAnswer = "A",
                Points = 1
            };

            var attempt = new QuizAttempt
            {
                Id = attemptId,
                TotalQuestions = 1,
                Quiz = new Quiz
                {
                    PassingScore = 50,
                    Questions = new List<QuizQuestion> { question }
                },
                QuizAnswers = new List<QuizAnswer>
                {
                    new QuizAnswer
                    {
                        QuizQuestionId = question.Id,
                        SelectedAnswer = "A"
                    }
                }
            };

            SetupQuizAttempts(new List<QuizAttempt> { attempt });

            _contextMock.Setup(x =>
                x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var command = new SubmitQuizCommand
            {
                AttemptId = attemptId
            };

            await _handler.Handle(command, CancellationToken.None);

            _contextMock.Verify(x =>
                x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}