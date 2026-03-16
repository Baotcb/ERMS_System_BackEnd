using ERMS.Application.Features.Lessons.Commands.UpdateLessonProgress;
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

namespace ERMS.UnitTests.Features.Lessons.Commands.UpdateLessonProgress
{
    public class UpdateLessonProgressHandlerTests
    {
        private readonly Mock<IERMSDbContext> _contextMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly UpdateLessonProgressHandler _handler;
        private readonly Guid _userId;
        private readonly Guid _employeeId;

        public UpdateLessonProgressHandlerTests()
        {
            _contextMock = new Mock<IERMSDbContext>();
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _userId = Guid.NewGuid();
            _employeeId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);

            _handler = new UpdateLessonProgressHandler(_contextMock.Object, _currentUserServiceMock.Object);
        }

        private void SetupData(
            List<ERMS.Domain.Entities.Organization.Employee> employees,
            List<Enrollment> enrollments,
            List<LessonProgress> progresses,
            List<Lesson> lessons)
        {
            _contextMock.Setup(x => x.Employees)
                .Returns(employees.AsQueryable().BuildMockDbSet().Object);

            _contextMock.Setup(x => x.Enrollments)
                .Returns(enrollments.AsQueryable().BuildMockDbSet().Object);

            _contextMock.Setup(x => x.LessonProgresses)
                .Returns(progresses.AsQueryable().BuildMockDbSet().Object);

            _contextMock.Setup(x => x.Lessons)
                .Returns(lessons.AsQueryable().BuildMockDbSet().Object);
        }

        [Fact]
        public async Task Handle_EnrollmentNotFound_ShouldThrowException()
        {
            SetupData(
                new List<ERMS.Domain.Entities.Organization.Employee>
                {
                    new()
                    {
                        Id = _employeeId,
                        UserId = _userId
                    }
                },
                new List<Enrollment>(),
                new List<LessonProgress>(),
                new List<Lesson>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        CourseId = Guid.NewGuid(),
                        IsDeleted = false
                    }
                });

            var lessonId = _contextMock.Object.Lessons.First().Id;

            var command = new UpdateLessonProgressCommand
            {
                LessonId = lessonId
            };

            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("Không tìm thấy đăng ký khóa học");
        }

        [Fact]
        public async Task Handle_CreateNewProgress_WhenProgressNotExist()
        {
            var enrollmentId = Guid.NewGuid();
            var lessonId = Guid.NewGuid();
            var courseId = Guid.NewGuid();

            var enrollment = new Enrollment
            {
                Id = enrollmentId,
                EmployeeId = _employeeId,
                CourseId = courseId
            };

            SetupData(
                new List<ERMS.Domain.Entities.Organization.Employee>
                {
                    new()
                    {
                        Id = _employeeId,
                        UserId = _userId
                    }
                },
                new List<Enrollment> { enrollment },
                new List<LessonProgress>(),
                new List<Lesson>
                {
                    new Lesson
                    {
                        Id = lessonId,
                        CourseId = courseId,
                        IsDeleted = false
                    }
                });

            _contextMock.Setup(x => x.LessonProgresses.Add(It.IsAny<LessonProgress>()));

            _contextMock.Setup(x =>
                x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var command = new UpdateLessonProgressCommand
            {
                LessonId = lessonId,
                WatchPercentage = 50,
                TimeSpentMinutes = 10
            };

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Should().BeTrue();

            _contextMock.Verify(x =>
                x.LessonProgresses.Add(It.IsAny<LessonProgress>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_CompleteLesson_WhenWatchPercentage100()
        {
            var enrollmentId = Guid.NewGuid();
            var lessonId = Guid.NewGuid();
            var courseId = Guid.NewGuid();

            var progress = new LessonProgress
            {
                EnrollmentId = enrollmentId,
                LessonId = lessonId,
                TimeSpentMinutes = 5
            };

            var enrollment = new Enrollment
            {
                Id = enrollmentId,
                EmployeeId = _employeeId,
                CourseId = courseId
            };

            SetupData(
                new List<ERMS.Domain.Entities.Organization.Employee>
                {
                    new()
                    {
                        Id = _employeeId,
                        UserId = _userId
                    }
                },
                new List<Enrollment> { enrollment },
                new List<LessonProgress> { progress },
                new List<Lesson>
                {
                    new Lesson
                    {
                        Id = lessonId,
                        CourseId = courseId,
                        IsDeleted = false
                    }
                });

            _contextMock.Setup(x =>
                x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var command = new UpdateLessonProgressCommand
            {
                LessonId = lessonId,
                WatchPercentage = 100,
                TimeSpentMinutes = 10
            };

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Should().BeTrue();

            progress.Status.Should().Be("Completed");
            progress.CompletedAt.Should().NotBeNull();
        }
    }
}