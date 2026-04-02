using ERMS.Application.Features.Quizzes.Commands.SubmitQuiz;
using ERMS.Domain.Entities.Training;
using ERMS.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Quizzes.Commands.SubmitQuiz
{
    public class SubmitQuizHandlerTests : IDisposable
    {
        private readonly ERMSDbContext _context;
        private readonly SubmitQuizHandler _handler;

        public SubmitQuizHandlerTests()
        {
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ERMSDbContext(options);
            _handler = new SubmitQuizHandler(_context);
        }

        [Fact]
        public async Task Handle_AttemptNotFound_ShouldThrowException()
        {
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

            var q1 = new QuizQuestion { Id = Guid.NewGuid(), CorrectAnswer = "A", Points = 1, QuestionText = "1", Options = "1" };
            var q2 = new QuizQuestion { Id = Guid.NewGuid(), CorrectAnswer = "B", Points = 1, QuestionText = "2", Options = "2" };

            var attempt = new QuizAttempt
            {
                Id = attemptId,
                EnrollmentId = Guid.NewGuid(),
                TotalQuestions = 2,
                Quiz = new Quiz
                {
                    PassingScore = 50,
                    Questions = new List<QuizQuestion> { q1, q2 },
                    QuizTitle = "Sample Quiz",

                },
                QuizAnswers = new List<QuizAnswer>
                {
                    new QuizAnswer { QuizQuestionId = q1.Id, SelectedAnswer = "A" },
                    new QuizAnswer { QuizQuestionId = q2.Id, SelectedAnswer = "B" }
                }
            };

            _context.QuizAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            var result = await _handler.Handle(
                new SubmitQuizCommand { AttemptId = attemptId },
                CancellationToken.None);

            result.Score.Should().Be(100);
            result.CorrectAnswers.Should().Be(2);
            result.TotalQuestions.Should().Be(2);
            result.IsPassed.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_PartialCorrect_ShouldCalculateScoreCorrectly()
        {
            var attemptId = Guid.NewGuid();

            var q1 = new QuizQuestion { Id = Guid.NewGuid(), CorrectAnswer = "A", Points = 1, QuestionText = "1", Options = "1" };
            var q2 = new QuizQuestion { Id = Guid.NewGuid(), CorrectAnswer = "B", Points = 1, QuestionText = "2", Options = "2" };


            var attempt = new QuizAttempt
            {
                Id = attemptId,
                EnrollmentId = Guid.NewGuid(),
                TotalQuestions = 2,
                Quiz = new Quiz
                {
                    PassingScore = 70,
                    Questions = new List<QuizQuestion> { q1, q2 },
                    QuizTitle = "Sample Quiz"
                },
                QuizAnswers = new List<QuizAnswer>
                {
                    new QuizAnswer { QuizQuestionId = q1.Id, SelectedAnswer = "A" },
                    new QuizAnswer { QuizQuestionId = q2.Id, SelectedAnswer = "C" }
                }
            };

            _context.QuizAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            var result = await _handler.Handle(
                new SubmitQuizCommand { AttemptId = attemptId },
                CancellationToken.None);

            result.Score.Should().Be(50);
            result.CorrectAnswers.Should().Be(1);
            result.IsPassed.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_ShouldUpdateEnrollment_WhenPassed()
        {
            var attemptId = Guid.NewGuid();
            var enrollmentId = Guid.NewGuid();

            var question = new QuizQuestion
            {
                Id = Guid.NewGuid(),
                CorrectAnswer = "A",
                Points = 1,
                Options = "A",
                QuestionText = "Sample Question"
            };

            var enrollment = new Enrollment
            {
                Id = enrollmentId,
                Status = "InProgress"
            };

            var attempt = new QuizAttempt
            {
                Id = attemptId,
                EnrollmentId = enrollmentId,
                TotalQuestions = 1,
                Quiz = new Quiz
                {
                    PassingScore = 50,
                    Questions = new List<QuizQuestion> { question },
                    QuizTitle = "Sample Quiz"
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

            _context.Enrollments.Add(enrollment);
            _context.QuizAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            await _handler.Handle(
                new SubmitQuizCommand { AttemptId = attemptId },
                CancellationToken.None);

            enrollment.Status.Should().Be("Completed");
            enrollment.CompletedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task Handle_ShouldSaveChanges()
        {
            var attemptId = Guid.NewGuid();

            var question = new QuizQuestion
            {
                Id = Guid.NewGuid(),
                CorrectAnswer = "A",
                Points = 1,
                Options = "A",
                QuestionText = "Sample Question"
            };

            var attempt = new QuizAttempt
            {
                Id = attemptId,
                EnrollmentId = Guid.NewGuid(),
                TotalQuestions = 1,
                Quiz = new Quiz
                {
                    PassingScore = 50,
                    Questions = new List<QuizQuestion> { question },
                    QuizTitle = "Sample Quiz"
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

            _context.QuizAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            await _handler.Handle(
                new SubmitQuizCommand { AttemptId = attemptId },
                CancellationToken.None);

            var updated = await _context.QuizAttempts.FindAsync(attemptId);
            updated!.Status.Should().Be("Completed");
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}