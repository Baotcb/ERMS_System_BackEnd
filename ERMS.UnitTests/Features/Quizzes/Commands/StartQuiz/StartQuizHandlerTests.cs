using ERMS.Application.Features.Quizzes.Commands.StartQuiz;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Organization;
using ERMS.Domain.Entities.Training;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Quizzes.Commands.StartQuiz
{
    public class StartQuizHandlerTests
    {
        private readonly Mock<IERMSDbContext> _contextMock = new();
        private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
        private readonly StartQuizHandler _handler;

        public StartQuizHandlerTests()
        {
            _handler = new StartQuizHandler(
                _contextMock.Object,
                _currentUserServiceMock.Object);
        }

        #region Helpers

        private void SetupBasicData(
            Guid userId,
            Guid employeeId,
            Guid enrollmentId,
            Guid courseId,
            Guid quizId,
            List<QuizAttempt>? attempts = null,
            int lessonCount = 1,
            bool completedAllLessons = true,
            int maxAttempts = 3,
            int timeLimitMinutes = 60)
        {
            var employees = new List<Employee>
            {
                new Employee { Id = employeeId, UserId = userId }
            };

            var enrollments = new List<Enrollment>
            {
                new Enrollment
                {
                    Id = enrollmentId,
                    EmployeeId = employeeId,
                    CourseId = courseId,
                    IsDeleted = false
                }
            };

            var lessons = Enumerable.Range(0, lessonCount)
                .Select(_ => new Lesson
                {
                    Id = Guid.NewGuid(),
                    CourseId = courseId,
                    IsDeleted = false
                }).ToList();

            var progresses = completedAllLessons
                ? lessons.Select(l => new LessonProgress
                {
                    EnrollmentId = enrollmentId,
                    LessonId = l.Id,
                    Status = "Completed"
                }).ToList()
                : new List<LessonProgress>();

            var quizzes = new List<Quiz>
            {
                new Quiz
                {
                    Id = quizId,
                    CourseId = courseId,
                    IsActive = true,
                    IsDeleted = false,
                    MaxAttempts = maxAttempts,
                    TimeLimitMinutes = timeLimitMinutes,
                    Questions = new List<QuizQuestion>
                    {
                        new QuizQuestion(),
                        new QuizQuestion()
                    }
                }
            };

            _contextMock.Setup(x => x.Employees)
                .Returns(employees.AsQueryable().BuildMockDbSet().Object);

            _contextMock.Setup(x => x.Enrollments)
                .Returns(enrollments.AsQueryable().BuildMockDbSet().Object);

            _contextMock.Setup(x => x.Quizzes)
                .Returns(quizzes.AsQueryable().BuildMockDbSet().Object);

            _contextMock.Setup(x => x.Lessons)
                .Returns(lessons.AsQueryable().BuildMockDbSet().Object);

            _contextMock.Setup(x => x.LessonProgresses)
                .Returns(progresses.AsQueryable().BuildMockDbSet().Object);

            attempts ??= new List<QuizAttempt>();

            var dbSetAttempts = attempts.AsQueryable().BuildMockDbSet();

            _contextMock.Setup(x => x.QuizAttempts)
                .Returns(dbSetAttempts.Object);

            _contextMock.Setup(x => x.QuizAttempts.Add(It.IsAny<QuizAttempt>()))
                .Callback<QuizAttempt>(a => attempts.Add(a));

            _contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
        }

        #endregion

        [Fact]
        public async Task Handle_ShouldThrow_WhenUserNotAuthenticated()
        {
            _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

            var command = new StartQuizCommand();

            await FluentActions
                .Invoking(() => _handler.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task Handle_ShouldThrow_WhenLessonsNotCompleted()
        {
            var userId = Guid.NewGuid();
            var employeeId = Guid.NewGuid();
            var enrollmentId = Guid.NewGuid();
            var courseId = Guid.NewGuid();
            var quizId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

            SetupBasicData(userId, employeeId, enrollmentId, courseId, quizId, completedAllLessons: false);

            var command = new StartQuizCommand
            {
                QuizId = quizId,
                CourseId = courseId
            };

            await FluentActions
                .Invoking(() => _handler.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("*hoàn thành tất cả các bài học*");
        }

        [Fact]
        public async Task Handle_ShouldBlock_WhenMaxAttemptsReached_AndStillInCooldown()
        {
            var userId = Guid.NewGuid();
            var employeeId = Guid.NewGuid();
            var enrollmentId = Guid.NewGuid();
            var courseId = Guid.NewGuid();
            var quizId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

            var attempts = new List<QuizAttempt>
            {
                new QuizAttempt
                {
                    Id = Guid.NewGuid(),
                    QuizId = quizId,
                    EnrollmentId = enrollmentId,
                    StartedAt = DateTime.UtcNow.AddMinutes(-10) // chưa đủ cooldown
                },
                new QuizAttempt
                {
                    Id = Guid.NewGuid(),
                    QuizId = quizId,
                    EnrollmentId = enrollmentId,
                    StartedAt = DateTime.UtcNow.AddMinutes(-20)
                },
                new QuizAttempt
                {
                    Id = Guid.NewGuid(),
                    QuizId = quizId,
                    EnrollmentId = enrollmentId,
                    StartedAt = DateTime.UtcNow.AddMinutes(-30)
                }
            };

            SetupBasicData(
                userId, employeeId, enrollmentId, courseId, quizId,
                attempts: attempts,
                maxAttempts: 3,
                timeLimitMinutes: 60);

            var command = new StartQuizCommand
            {
                QuizId = quizId,
                CourseId = courseId
            };

            await FluentActions
                .Invoking(() => _handler.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("*Vui lòng thử lại sau*");
        }

        [Fact]
        public async Task Handle_ShouldAllow_WhenMaxAttemptsReached_ButCooldownPassed()
        {
            var userId = Guid.NewGuid();
            var employeeId = Guid.NewGuid();
            var enrollmentId = Guid.NewGuid();
            var courseId = Guid.NewGuid();
            var quizId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

            var attempts = new List<QuizAttempt>
            {
                new QuizAttempt
                {
                    Id = Guid.NewGuid(),
                    QuizId = quizId,
                    EnrollmentId = enrollmentId,
                    StartedAt = DateTime.UtcNow.AddMinutes(-120) // đã qua cooldown
                },
                new QuizAttempt
                {
                    Id = Guid.NewGuid(),
                    QuizId = quizId,
                    EnrollmentId = enrollmentId,
                    StartedAt = DateTime.UtcNow.AddMinutes(-130)
                },
                new QuizAttempt
                {
                    Id = Guid.NewGuid(),
                    QuizId = quizId,
                    EnrollmentId = enrollmentId,
                    StartedAt = DateTime.UtcNow.AddMinutes(-140)
                }
            };

            SetupBasicData(
                userId, employeeId, enrollmentId, courseId, quizId,
                attempts: attempts,
                maxAttempts: 3,
                timeLimitMinutes: 60);

            var command = new StartQuizCommand
            {
                QuizId = quizId,
                CourseId = courseId
            };

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Should().NotBeNull();
            result.AttemptId.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public async Task Handle_ShouldStartQuizSuccessfully()
        {
            var userId = Guid.NewGuid();
            var employeeId = Guid.NewGuid();
            var enrollmentId = Guid.NewGuid();
            var courseId = Guid.NewGuid();
            var quizId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

            var attempts = new List<QuizAttempt>();

            SetupBasicData(
                userId, employeeId, enrollmentId, courseId, quizId,
                attempts: attempts);

            var command = new StartQuizCommand
            {
                QuizId = quizId,
                CourseId = courseId
            };

            var result = await _handler.Handle(command, CancellationToken.None);

            result.AttemptId.Should().NotBe(Guid.Empty);
            attempts.Should().HaveCount(1);
            attempts[0].Status.Should().Be("InProgress");
        }
    }
}