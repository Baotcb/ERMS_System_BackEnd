using ERMS.Application.Features.Quizzes.Commands.StartQuiz;
using ERMS.Application.Interface;
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

namespace ERMS.UnitTests.Features.Quizzes.Commands.StartQuiz
{
    public class StartQuizCommandHandlerTests
    {
        private readonly Mock<IERMSDbContext> _contextMock;
        private readonly Mock<ICurrentUserService> _currentUserMock;
        private readonly StartQuizCommandHandler _handler;

        public StartQuizCommandHandlerTests()
        {
            _contextMock = new Mock<IERMSDbContext>();
            _currentUserMock = new Mock<ICurrentUserService>();

            _handler = new StartQuizCommandHandler(
                _contextMock.Object,
                _currentUserMock.Object);
        }

        [Fact]
        public async Task Handle_UserNotAuthenticated_ShouldThrowException()
        {
            _currentUserMock.Setup(x => x.UserId).Returns((Guid?)null);

            var command = new StartQuizCommand
            {
                QuizId = Guid.NewGuid()
            };

            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task Handle_QuizNotFound_ShouldThrowException()
        {
            var userId = Guid.NewGuid();
            var employeeId = Guid.NewGuid();

            _currentUserMock.Setup(x => x.UserId).Returns(userId);

            var employees = new List<Employee>
            {
                new Employee { Id = employeeId, UserId = userId }
            };

            var enrollments = new List<Enrollment>
            {
                new Enrollment { Id = Guid.NewGuid(), EmployeeId = employeeId }
            };

            _contextMock.Setup(x => x.Employees)
                .Returns(employees.AsQueryable().BuildMockDbSet().Object);

            _contextMock.Setup(x => x.Enrollments)
                .Returns(enrollments.AsQueryable().BuildMockDbSet().Object);

            _contextMock.Setup(x => x.Quizzes)
                .Returns(new List<Quiz>().AsQueryable().BuildMockDbSet().Object);

            var command = new StartQuizCommand
            {
                QuizId = Guid.NewGuid()
            };

            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Quiz not found");
        }

        [Fact]
        public async Task Handle_ValidRequest_ShouldCreateAttempt()
        {
            var userId = Guid.NewGuid();
            var employeeId = Guid.NewGuid();
            var enrollmentId = Guid.NewGuid();
            var quizId = Guid.NewGuid();

            _currentUserMock.Setup(x => x.UserId).Returns(userId);

            var employees = new List<Employee>
            {
                new Employee { Id = employeeId, UserId = userId }
            };

            var enrollments = new List<Enrollment>
            {
                new Enrollment { Id = enrollmentId, EmployeeId = employeeId }
            };

            var quizzes = new List<Quiz>
            {
                new Quiz
                {
                    Id = quizId,
                    IsActive = true,
                    Questions = new List<QuizQuestion>
                    {
                        new QuizQuestion(),
                        new QuizQuestion()
                    }
                }
            };

            var attempts = new List<QuizAttempt>();

            _contextMock.Setup(x => x.Employees)
                .Returns(employees.AsQueryable().BuildMockDbSet().Object);

            _contextMock.Setup(x => x.Enrollments)
                .Returns(enrollments.AsQueryable().BuildMockDbSet().Object);

            _contextMock.Setup(x => x.Quizzes)
                .Returns(quizzes.AsQueryable().BuildMockDbSet().Object);

            _contextMock.Setup(x => x.QuizAttempts)
                .Returns(attempts.AsQueryable().BuildMockDbSet().Object);

            _contextMock.Setup(x => x.QuizAttempts.Add(It.IsAny<QuizAttempt>()));

            _contextMock.Setup(x =>
                x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var command = new StartQuizCommand
            {
                QuizId = quizId
            };

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Should().NotBeEmpty();

            _contextMock.Verify(x =>
                x.QuizAttempts.Add(It.IsAny<QuizAttempt>()),
                Times.Once);
        }
    }
}