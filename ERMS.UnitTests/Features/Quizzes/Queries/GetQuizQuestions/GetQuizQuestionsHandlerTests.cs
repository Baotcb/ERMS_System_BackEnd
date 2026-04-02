using ERMS.Application.Features.Quizzes.Queries.GetQuizQuestions;
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

namespace ERMS.UnitTests.Features.Quizzes.Queries.GetQuizQuestions
{
    public class GetQuizQuestionsHandlerTests
    {
        private readonly Mock<IERMSDbContext> _contextMock;
        private readonly GetQuizQuestionsHandler _handler;

        public GetQuizQuestionsHandlerTests()
        {
            _contextMock = new Mock<IERMSDbContext>();
            _handler = new GetQuizQuestionsHandler(_contextMock.Object);
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

            var query = new GetQuizQuestionsQuery
            {
                AttemptId = Guid.NewGuid()
            };

            Func<Task> act = () => _handler.Handle(query, CancellationToken.None);

            await act.Should()
                .ThrowAsync<Exception>()
                .WithMessage("Không tìm thấy lượt làm bài");
        }

        [Fact]
        public async Task Handle_ValidAttempt_ShouldReturnQuestions()
        {
            var attemptId = Guid.NewGuid();

            var quiz = new Quiz
            {
                Questions = new List<QuizQuestion>
                {
                    new QuizQuestion
                    {
                        Id = Guid.NewGuid(),
                        QuestionText = "Question 1",
                        Options = "A,B,C,D",
                        OrderIndex = 1,
                        IsActive = true
                    },
                    new QuizQuestion
                    {
                        Id = Guid.NewGuid(),
                        QuestionText = "Question 2",
                        Options = "A,B,C,D",
                        OrderIndex = 2,
                        IsActive = true
                    }
                }
            };

            SetupQuizAttempts(new List<QuizAttempt>
            {
                new QuizAttempt
                {
                    Id = attemptId,
                    Quiz = quiz
                }
            });

            var query = new GetQuizQuestionsQuery
            {
                AttemptId = attemptId
            };

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Should().HaveCount(2);
            result[0].QuestionText.Should().Be("Question 1");
            result[1].QuestionText.Should().Be("Question 2");
        }

        [Fact]
        public async Task Handle_ShouldReturnOnlyActiveQuestions()
        {
            var attemptId = Guid.NewGuid();

            var quiz = new Quiz
            {
                Questions = new List<QuizQuestion>
                {
                    new QuizQuestion
                    {
                        Id = Guid.NewGuid(),
                        QuestionText = "Active Question",
                        Options = "A,B,C,D",
                        OrderIndex = 1,
                        IsActive = true
                    },
                    new QuizQuestion
                    {
                        Id = Guid.NewGuid(),
                        QuestionText = "Inactive Question",
                        Options = "A,B,C,D",
                        OrderIndex = 2,
                        IsActive = false
                    }
                }
            };

            SetupQuizAttempts(new List<QuizAttempt>
            {
                new QuizAttempt
                {
                    Id = attemptId,
                    Quiz = quiz
                }
            });

            var query = new GetQuizQuestionsQuery
            {
                AttemptId = attemptId
            };

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Should().HaveCount(1);
            result[0].QuestionText.Should().Be("Active Question");
        }

        [Fact]
        public async Task Handle_ShouldOrderQuestionsByOrderIndex()
        {
            var attemptId = Guid.NewGuid();

            var quiz = new Quiz
            {
                Questions = new List<QuizQuestion>
                {
                    new QuizQuestion
                    {
                        Id = Guid.NewGuid(),
                        QuestionText = "Question 2",
                        Options = "A,B,C,D",
                        OrderIndex = 2,
                        IsActive = true
                    },
                    new QuizQuestion
                    {
                        Id = Guid.NewGuid(),
                        QuestionText = "Question 1",
                        Options = "A,B,C,D",
                        OrderIndex = 1,
                        IsActive = true
                    }
                }
            };

            SetupQuizAttempts(new List<QuizAttempt>
            {
                new QuizAttempt
                {
                    Id = attemptId,
                    Quiz = quiz
                }
            });

            var query = new GetQuizQuestionsQuery
            {
                AttemptId = attemptId
            };

            var result = await _handler.Handle(query, CancellationToken.None);

            result[0].OrderIndex.Should().Be(1);
            result[1].OrderIndex.Should().Be(2);
        }
    }
}