using ERMS.Application.Features.Quizzes.Queries.GetQuizResult;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Organization;
using ERMS.Domain.Entities.Training;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Quizzes.Queries.GetQuizResult
{


    public class GetQuizResultHandlerTests
    {
        private readonly Mock<IERMSDbContext> _contextMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly GetQuizResultHandler _handler;

        public GetQuizResultHandlerTests()
        {
            _contextMock = new Mock<IERMSDbContext>();
            _currentUserServiceMock = new Mock<ICurrentUserService>();

            _handler = new GetQuizResultHandler(
                _contextMock.Object,
                _currentUserServiceMock.Object);
        }

        private void SetupContext(
            List<Employee> employees,
            List<Enrollment> enrollments,
            List<Quiz> quizzes,
            List<QuizAttempt> attempts)
        {
            _contextMock.Setup(x => x.Employees)
                .Returns(employees.AsQueryable().BuildMockDbSet().Object);

            _contextMock.Setup(x => x.Enrollments)
                .Returns(enrollments.AsQueryable().BuildMockDbSet().Object);

            _contextMock.Setup(x => x.Quizzes)
                .Returns(quizzes.AsQueryable().BuildMockDbSet().Object);

            _contextMock.Setup(x => x.QuizAttempts)
                .Returns(attempts.AsQueryable().BuildMockDbSet().Object);
        }

        [Fact]
        public async Task Handle_UserNotLoggedIn_ReturnsNull()
        {
            _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

            var result = await _handler.Handle(
                new GetQuizResultQuery { CourseId = Guid.NewGuid() },
                CancellationToken.None);

            result.Should().BeNull();
        }

        [Fact]
        public async Task Handle_NoEmployee_ReturnsNull()
        {
            var userId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

            SetupContext(
                new List<Employee>(),
                new List<Enrollment>(),
                new List<Quiz>(),
                new List<QuizAttempt>());

            var result = await _handler.Handle(
                new GetQuizResultQuery { CourseId = Guid.NewGuid() },
                CancellationToken.None);

            result.Should().BeNull();
        }

        [Fact]
        public async Task Handle_NoEnrollment_ReturnsNull()
        {
            var userId = Guid.NewGuid();
            var employee = new Employee { Id = Guid.NewGuid(), UserId = userId };

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

            SetupContext(
                new List<Employee> { employee },
                new List<Enrollment>(),
                new List<Quiz>(),
                new List<QuizAttempt>());

            var result = await _handler.Handle(
                new GetQuizResultQuery { CourseId = Guid.NewGuid() },
                CancellationToken.None);

            result.Should().BeNull();
        }

        [Fact]
        public async Task Handle_NoQuiz_ReturnsNull()
        {
            var userId = Guid.NewGuid();
            var employee = new Employee { Id = Guid.NewGuid(), UserId = userId };
            var enrollment = new Enrollment
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                CourseId = Guid.NewGuid(),
                IsDeleted = false
            };

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

            SetupContext(
                new List<Employee> { employee },
                new List<Enrollment> { enrollment },
                new List<Quiz>(),
                new List<QuizAttempt>());

            var result = await _handler.Handle(
                new GetQuizResultQuery { CourseId = enrollment.CourseId },
                CancellationToken.None);

            result.Should().BeNull();
        }

        [Fact]
        public async Task Handle_ReturnsCorrectResult()
        {
            var userId = Guid.NewGuid();
            var employee = new Employee { Id = Guid.NewGuid(), UserId = userId };
            var courseId = Guid.NewGuid();

            var enrollment = new Enrollment
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                CourseId = courseId,
                IsDeleted = false
            };

            var quiz = new Quiz
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                IsActive = true,
                IsDeleted = false,
                TimeLimitMinutes = 30,
                MaxAttempts = 3
            };

            var attempt = new QuizAttempt
            {
                Id = Guid.NewGuid(),
                QuizId = quiz.Id,
                EnrollmentId = enrollment.Id,
                StartedAt = DateTime.UtcNow.AddMinutes(-10),
                Score = 80,
                IsPassed = true,
                CorrectAnswers = 8,
                TotalQuestions = 10,
                CompletedAt = DateTime.UtcNow
            };

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

            SetupContext(
                new List<Employee> { employee },
                new List<Enrollment> { enrollment },
                new List<Quiz> { quiz },
                new List<QuizAttempt> { attempt });

            var result = await _handler.Handle(
                new GetQuizResultQuery { CourseId = courseId },
                CancellationToken.None);

            result.Should().NotBeNull();
            result.Score.Should().Be(80);
            result.IsPassed.Should().BeTrue();
            result.AttemptCount.Should().Be(1);
            result.NextAvailableTime.Should().NotBeNull();
        }

        [Fact]
        public async Task Handle_ShouldCalculateNextAvailableTimeCorrectly()
        {
            var userId = Guid.NewGuid();
            var employee = new Employee { Id = Guid.NewGuid(), UserId = userId };
            var courseId = Guid.NewGuid();

            var enrollment = new Enrollment
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                CourseId = courseId,
                IsDeleted = false
            };

            var quiz = new Quiz
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                IsActive = true,
                IsDeleted = false,
                TimeLimitMinutes = 30
            };

            var startTime = DateTime.UtcNow;

            var attempt = new QuizAttempt
            {
                Id = Guid.NewGuid(),
                QuizId = quiz.Id,
                EnrollmentId = enrollment.Id,
                StartedAt = startTime
            };

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

            SetupContext(
                new List<Employee> { employee },
                new List<Enrollment> { enrollment },
                new List<Quiz> { quiz },
                new List<QuizAttempt> { attempt });

            var result = await _handler.Handle(
                new GetQuizResultQuery { CourseId = courseId },
                CancellationToken.None);

            result.NextAvailableTime.Should()
                .BeCloseTo(startTime.AddMinutes(30), TimeSpan.FromSeconds(1));
        }
    }
}
