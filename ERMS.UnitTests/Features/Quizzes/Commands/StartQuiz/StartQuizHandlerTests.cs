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

        [Fact]
        public async Task Handle_ShouldThrow_WhenUserNotAuthenticated()
        {
            _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

            var command = new StartQuizCommand
            {
                QuizId = Guid.NewGuid(),
                CourseId = Guid.NewGuid()
            };

            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task Handle_ShouldThrow_WhenEnrollmentNotFound()
        {
            var userId = Guid.NewGuid();
            var courseId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

            var employees = new List<Employee>
            {
                new Employee { Id = Guid.NewGuid(), UserId = userId }
            };

            _contextMock.Setup(x => x.Employees)
                .Returns(employees.AsQueryable().BuildMockDbSet().Object);

            _contextMock.Setup(x => x.Enrollments)
                .Returns(new List<Enrollment>()
                .AsQueryable().BuildMockDbSet().Object);

            var command = new StartQuizCommand
            {
                QuizId = Guid.NewGuid(),
                CourseId = courseId
            };

            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Người dùng chưa đăng ký khóa học");
        }

        [Fact]
        public async Task Handle_ShouldThrow_WhenQuizNotFound()
        {
            var userId = Guid.NewGuid();
            var employeeId = Guid.NewGuid();
            var courseId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

            var employees = new List<Employee>
            {
                new Employee { Id = employeeId, UserId = userId }
            };

            var enrollments = new List<Enrollment>
            {
                new Enrollment
                {
                    Id = Guid.NewGuid(),
                    EmployeeId = employeeId,
                    CourseId = courseId,
                    IsDeleted = false
                }
            };

            _contextMock.Setup(x => x.Employees)
                .Returns(employees.AsQueryable().BuildMockDbSet().Object);

            _contextMock.Setup(x => x.Enrollments)
                .Returns(enrollments.AsQueryable().BuildMockDbSet().Object);

            _contextMock.Setup(x => x.Quizzes)
                .Returns(new List<Quiz>()
                .AsQueryable().BuildMockDbSet().Object);

            var command = new StartQuizCommand
            {
                QuizId = Guid.NewGuid(),
                CourseId = courseId
            };

            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Không tìm thấy bài kiểm tra");
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

            var quizzes = new List<Quiz>
            {
                new Quiz
                {
                    Id = quizId,
                    CourseId = courseId,
                    IsActive = true,
                    IsDeleted = false,
                    Questions = new List<QuizQuestion>()
                }
            };

            var lessons = new List<Lesson>
            {
                new Lesson { Id = Guid.NewGuid(), CourseId = courseId, IsDeleted = false },
                new Lesson { Id = Guid.NewGuid(), CourseId = courseId, IsDeleted = false }
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
                .Returns(new List<LessonProgress>()
                .AsQueryable().BuildMockDbSet().Object);

            var command = new StartQuizCommand
            {
                QuizId = quizId,
                CourseId = courseId
            };

            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Bạn phải hoàn thành tất cả các bài học trong khóa học trước khi làm bài kiểm tra");
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

            var lessons = new List<Lesson>
            {
                new Lesson { Id = Guid.NewGuid(), CourseId = courseId, IsDeleted = false }
            };

            var quizzes = new List<Quiz>
            {
                new Quiz
                {
                    Id = quizId,
                    CourseId = courseId,
                    IsActive = true,
                    IsDeleted = false,
                    Questions = new List<QuizQuestion>
                    {
                        new QuizQuestion(),
                        new QuizQuestion()
                    }
                }
            };

            var progresses = new List<LessonProgress>
            {
                new LessonProgress
                {
                    EnrollmentId = enrollmentId,
                    LessonId = lessons[0].Id,
                    Status = "Completed"
                }
            };

            var attempts = new List<QuizAttempt>();

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

            var dbSetAttempts = attempts.AsQueryable().BuildMockDbSet();

            _contextMock.Setup(x => x.QuizAttempts)
                .Returns(dbSetAttempts.Object);

            _contextMock.Setup(x => x.QuizAttempts.Add(It.IsAny<QuizAttempt>()))
                .Callback<QuizAttempt>(a => attempts.Add(a));

            _contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var command = new StartQuizCommand
            {
                QuizId = quizId,
                CourseId = courseId
            };

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Should().NotBe(Guid.Empty);
            attempts.Should().HaveCount(1);
            attempts[0].Status.Should().Be("InProgress");
        }
    }
}