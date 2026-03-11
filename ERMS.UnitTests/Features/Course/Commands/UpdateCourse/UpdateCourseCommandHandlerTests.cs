using ERMS.Application.Features.Courses.Commands.UpdateCourse;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Organization;
using ERMS.Domain.Entities.Training;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Courses.Commands
{
    public class UpdateCourseCommandHandlerTests
    {
        private readonly Mock<IERMSDbContext> _contextMock = new();
        private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
        private readonly Mock<ILogger<UpdateCourseCommandHandler>> _loggerMock = new();

        private readonly UpdateCourseCommandHandler _handler;

        public UpdateCourseCommandHandlerTests()
        {
            _handler = new UpdateCourseCommandHandler(
                _contextMock.Object,
                _currentUserServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorized_WhenUserNotLoggedIn()
        {
            _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

            var command = new UpdateCourseCommand
            {
                Id = Guid.NewGuid()
            };

            Func<Task> act = async () =>
                await _handler.Handle(command, CancellationToken.None);

            await act.Should()
                .ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Người dùng chưa đăng nhập.");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenEnterpriseNotFound()
        {
            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync((Guid?)null);

            var command = new UpdateCourseCommand
            {
                Id = Guid.NewGuid()
            };

            Func<Task> act = async () =>
                await _handler.Handle(command, CancellationToken.None);

            await act.Should()
                .ThrowAsync<Exception>()
                .WithMessage("Người dùng không thuộc doanh nghiệp nào.");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenCourseNotFound()
        {
            var enterpriseId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            var courses = new List<Course>();

            _contextMock.Setup(x => x.Courses)
                .Returns(courses.AsQueryable().BuildMockDbSet().Object);

            var command = new UpdateCourseCommand
            {
                Id = Guid.NewGuid()
            };

            Func<Task> act = async () =>
                await _handler.Handle(command, CancellationToken.None);

            await act.Should()
                .ThrowAsync<Exception>()
                .WithMessage("Không tìm thấy khóa học.");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenCourseCodeDuplicated()
        {
            var enterpriseId = Guid.NewGuid();
            var courseId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            var courses = new List<Course>
            {
                new Course
                {
                    Id = courseId,
                    EnterpriseId = enterpriseId,
                    CourseCode = "OLD01"
                },
                new Course
                {
                    Id = Guid.NewGuid(),
                    EnterpriseId = enterpriseId,
                    CourseCode = "NEW01"
                }
            };

            _contextMock.Setup(x => x.Courses)
                .Returns(courses.AsQueryable().BuildMockDbSet().Object);

            var command = new UpdateCourseCommand
            {
                Id = courseId,
                CourseCode = "NEW01"
            };

            Func<Task> act = async () =>
                await _handler.Handle(command, CancellationToken.None);

            await act.Should()
                .ThrowAsync<Exception>()
                .WithMessage("Mã khóa học đã tồn tại.");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenTrainerNotFound()
        {
            var enterpriseId = Guid.NewGuid();
            var courseId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            var courses = new List<Course>
            {
                new Course
                {
                    Id = courseId,
                    EnterpriseId = enterpriseId,
                    CourseCode = "OLD01"
                }
            };

            _contextMock.Setup(x => x.Courses)
                .Returns(courses.AsQueryable().BuildMockDbSet().Object);

            var employees = new List<Employee>();

            _contextMock.Setup(x => x.Employees)
                .Returns(employees.AsQueryable().BuildMockDbSet().Object);

            var command = new UpdateCourseCommand
            {
                Id = courseId,
                CourseCode = "NEW01",
                TrainerId = Guid.NewGuid()
            };

            Func<Task> act = async () =>
                await _handler.Handle(command, CancellationToken.None);

            await act.Should()
                .ThrowAsync<Exception>()
                .WithMessage("Không tìm thấy giảng viên.");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenEmployeeNotTrainer()
        {
            var enterpriseId = Guid.NewGuid();
            var courseId = Guid.NewGuid();
            var trainerUserId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            var courses = new List<Course>
            {
                new Course
                {
                    Id = courseId,
                    EnterpriseId = enterpriseId,
                    CourseCode = "OLD01"
                }
            };

            _contextMock.Setup(x => x.Courses)
                .Returns(courses.AsQueryable().BuildMockDbSet().Object);

            var employees = new List<Employee>
            {
                new Employee
                {
                    Id = Guid.NewGuid(),
                    UserId = trainerUserId,
                    EnterpriseId = enterpriseId,
                    IsTrainer = false
                }
            };

            _contextMock.Setup(x => x.Employees)
                .Returns(employees.AsQueryable().BuildMockDbSet().Object);

            var command = new UpdateCourseCommand
            {
                Id = courseId,
                CourseCode = "NEW01",
                TrainerId = trainerUserId
            };

            Func<Task> act = async () =>
                await _handler.Handle(command, CancellationToken.None);

            await act.Should()
                .ThrowAsync<Exception>()
                .WithMessage("Nhân viên này không phải là giảng viên.");
        }

        [Fact]
        public async Task Handle_ShouldUpdateCourseSuccessfully()
        {
            var enterpriseId = Guid.NewGuid();
            var courseId = Guid.NewGuid();
            var trainerUserId = Guid.NewGuid();
            var trainerId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            var courses = new List<Course>
            {
                new Course
                {
                    Id = courseId,
                    EnterpriseId = enterpriseId,
                    CourseCode = "OLD01"
                }
            };

            _contextMock.Setup(x => x.Courses)
                .Returns(courses.AsQueryable().BuildMockDbSet().Object);

            var employees = new List<Employee>
            {
                new Employee
                {
                    Id = trainerId,
                    UserId = trainerUserId,
                    EnterpriseId = enterpriseId,
                    IsTrainer = true
                }
            };

            _contextMock.Setup(x => x.Employees)
                .Returns(employees.AsQueryable().BuildMockDbSet().Object);

            _contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var command = new UpdateCourseCommand
            {
                Id = courseId,
                CourseName = "Updated Course",
                CourseCode = "NEW01",
                TrainerId = trainerUserId,
                CompletionCriteria = "Quiz"
            };

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Should().Be(courseId);
            courses[0].CourseCode.Should().Be("NEW01");
            courses[0].CourseName.Should().Be("Updated Course");
            courses[0].TrainerId.Should().Be(trainerId);
            courses[0].UpdatedAt.Should().NotBeNull();
        }
    }
}