using ERMS.Application.Features.Enrollments.Commands.AssignEmployeesToCourse;
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

namespace ERMS.UnitTests.Features.Enrollments.Commands
{
    public class AssignEmployeesToCourseHandlerTests
    {
        private readonly Mock<IERMSDbContext> _contextMock = new();
        private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();

        private readonly AssignEmployeesToCourseHandler _handler;

        public AssignEmployeesToCourseHandlerTests()
        {
            _handler = new AssignEmployeesToCourseHandler(
                _contextMock.Object,
                _currentUserServiceMock.Object);
        }

        [Fact]
        public async Task Handle_AssignEmployeesSuccessfully()
        {
            var enterpriseId = Guid.NewGuid();
            var courseId = Guid.NewGuid();
            var employee1 = Guid.NewGuid();
            var employee2 = Guid.NewGuid();

            _currentUserServiceMock
                .Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            var courses = new List<Course>
            {
                new Course
                {
                    Id = courseId,
                    EnterpriseId = enterpriseId
                }
            };

            _contextMock.Setup(x => x.Courses)
                .Returns(courses.AsQueryable().BuildMockDbSet().Object);

            var enrollments = new List<Enrollment>();

            _contextMock.Setup(x => x.Enrollments)
                .Returns(enrollments.AsQueryable().BuildMockDbSet().Object);

            var command = new AssignEmployeesToCourseCommand
            {
                CourseId = courseId,
                EmployeeIds = new List<Guid> { employee1, employee2 }
            };

            var result = await _handler.Handle(command, CancellationToken.None);

            result.TotalAssigned.Should().Be(2);
            result.AssignedEmployeeIds.Should().Contain(employee1);
            result.AssignedEmployeeIds.Should().Contain(employee2);
        }

        [Fact]
        public async Task Handle_CourseNotFound_ShouldThrowException()
        {
            var enterpriseId = Guid.NewGuid();

            _currentUserServiceMock
                .Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            var courses = new List<Course>();

            _contextMock.Setup(x => x.Courses)
                .Returns(courses.AsQueryable().BuildMockDbSet().Object);

            var enrollments = new List<Enrollment>();

            _contextMock.Setup(x => x.Enrollments)
                .Returns(enrollments.AsQueryable().BuildMockDbSet().Object);

            var command = new AssignEmployeesToCourseCommand
            {
                CourseId = Guid.NewGuid(),
                EmployeeIds = new List<Guid> { Guid.NewGuid() }
            };

            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("Không tìm thấy khóa học");
        }

        [Fact]
        public async Task Handle_SkipExistingEnrollment()
        {
            var enterpriseId = Guid.NewGuid();
            var courseId = Guid.NewGuid();
            var employee1 = Guid.NewGuid();
            var employee2 = Guid.NewGuid();

            _currentUserServiceMock
                .Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            var courses = new List<Course>
    {
        new Course
        {
            Id = courseId,
            EnterpriseId = enterpriseId
        }
    };

            _contextMock.Setup(x => x.Courses)
                .Returns(courses.AsQueryable().BuildMockDbSet().Object);

            var enrollments = new List<Enrollment>
    {
        new Enrollment
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            EmployeeId = employee1
        }
    };

            _contextMock.Setup(x => x.Enrollments)
                .Returns(enrollments.AsQueryable().BuildMockDbSet().Object);

            var command = new AssignEmployeesToCourseCommand
            {
                CourseId = courseId,
                EmployeeIds = new List<Guid> { employee1, employee2 }
            };

            var result = await _handler.Handle(command, CancellationToken.None);

            result.TotalAssigned.Should().Be(1);
            result.AssignedEmployeeIds.Should().Contain(employee2);
            result.SkippedEmployeeIds.Should().Contain(employee1);
        }
    }
}