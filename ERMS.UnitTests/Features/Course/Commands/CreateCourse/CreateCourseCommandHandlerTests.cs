using ERMS.Application.Features.Courses.Commands.CreateCourse;
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
    public class CreateCourseCommandHandlerTests
    {
        private readonly Mock<IERMSDbContext> _contextMock = new();
        private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
        private readonly Mock<ILogger<CreateCourseCommandHandler>> _loggerMock = new();

        private readonly CreateCourseCommandHandler _handler;

        public CreateCourseCommandHandlerTests()
        {
            _handler = new CreateCourseCommandHandler(
                _contextMock.Object,
                _currentUserServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorized_WhenUserNotAuthenticated()
        {
            _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

            var command = new CreateCourseCommand
            {
                CourseName = "Test",
                CourseCode = "C01"
            };

            Func<Task> act = async () =>
                await _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Người dùng chưa được xác thực");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenEnterpriseNotFound()
        {
            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync((Guid?)null);

            var command = new CreateCourseCommand
            {
                CourseName = "Test",
                CourseCode = "C01"
            };

            Func<Task> act = async () =>
                await _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Người dùng không thuộc doanh nghiệp nào");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenCourseCodeDuplicate()
        {
            var enterpriseId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            var courses = new List<Course>
            {
                new Course
                {
                    Id = Guid.NewGuid(),
                    CourseCode = "ASP01",
                    EnterpriseId = enterpriseId,
                    IsDeleted = false
                }
            };

            _contextMock.Setup(x => x.Courses)
                .Returns(courses.AsQueryable().BuildMockDbSet().Object);

            var command = new CreateCourseCommand
            {
                CourseName = "ASP.NET",
                CourseCode = "ASP01"
            };

            Func<Task> act = async () =>
                await _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Mã khóa học đã tồn tại");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenTrainerNotFound()
        {
            var enterpriseId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            var courses = new List<Course>();

            _contextMock.Setup(x => x.Courses)
                .Returns(courses.AsQueryable().BuildMockDbSet().Object);

            var employees = new List<Employee>();

            _contextMock.Setup(x => x.Employees)
                .Returns(employees.AsQueryable().BuildMockDbSet().Object);

            var command = new CreateCourseCommand
            {
                CourseName = "ASP.NET",
                CourseCode = "ASP01",
                TrainerId = Guid.NewGuid()
            };

            Func<Task> act = async () =>
                await _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Không tìm thấy giảng viên");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenEmployeeNotTrainer()
        {
            var enterpriseId = Guid.NewGuid();
            var trainerUserId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            var courses = new List<Course>();

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

            var command = new CreateCourseCommand
            {
                CourseName = "ASP.NET",
                CourseCode = "ASP01",
                TrainerId = trainerUserId
            };

            Func<Task> act = async () =>
                await _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Nhân viên này không phải là giảng viên");
        }

        [Fact]
        public async Task Handle_ShouldCreateCourseSuccessfully()
        {
            var enterpriseId = Guid.NewGuid();
            var trainerUserId = Guid.NewGuid();
            var trainerId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            var courses = new List<Course>();

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

            var command = new CreateCourseCommand
            {
                CourseName = "ASP.NET Core",
                CourseCode = "ASP01",
                TrainerId = trainerUserId,
                CompletionCriteria = "Quiz"
            };

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Should().NotBeEmpty();
        }
    }
}