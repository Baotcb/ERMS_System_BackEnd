using ERMS.Application.Features.Enrollments.Commands.AssignEmployeesToCourse;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Identity; // Giả sử chứa User entity
using ERMS.Domain.Entities.Organization;
using ERMS.Domain.Entities.Training;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Enrollments.Commands
{
    public class AssignEmployeesToCourseHandlerTests
    {
        private readonly Mock<IERMSDbContext> _contextMock = new();
        private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
        private readonly Mock<IZoomService> _zoomServiceMock = new();
        private readonly Mock<IEmailService> _emailServiceMock = new();

        private readonly AssignEmployeesToCourseHandler _handler;

        public AssignEmployeesToCourseHandlerTests()
        {
            _handler = new AssignEmployeesToCourseHandler(
                _contextMock.Object,
                _currentUserServiceMock.Object,
                _zoomServiceMock.Object,
                _emailServiceMock.Object);
        }

       
        [Fact]
        public async Task Handle_AssignSuccessfully_WithValidDepartment_ShouldSendEmails()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            var courseId = Guid.NewGuid();
            var trainingPlanId = Guid.NewGuid();
            var employeeId = Guid.NewGuid();
            var trainerEmail = "trainer@company.com";
            var traineeEmail = "trainee@company.com";
            var departmentId = 1;

            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            // Course
            var courses = new List<Course>
    {
        new Course {
            Id = courseId,
            EnterpriseId = enterpriseId,
            TrainingPlanId = trainingPlanId,
            CourseName = "Test Course",
            TrainerEmail = trainerEmail,
            IsOnline = true,
            Status = "Published"
        }
    }.AsQueryable().BuildMockDbSet();
            _contextMock.Setup(x => x.Courses).Returns(courses.Object);

            //  TrainingRequest (QUAN TRỌNG)
            var trainingRequests = new List<TrainingRequest>
    {
        new TrainingRequest {
            TrainingPlanId = trainingPlanId,
            DepartmentId = departmentId,
            IsDeleted = false
        }
    }.AsQueryable().BuildMockDbSet();
            _contextMock.Setup(x => x.TrainingRequests).Returns(trainingRequests.Object);

            // Employee hợp lệ
            var employees = new List<Employee>
    {
        new Employee {
            Id = employeeId,
            EnterpriseId = enterpriseId,
            DepartmentId = departmentId,
            User = new User { Email = traineeEmail }
        }
    }.AsQueryable().BuildMockDbSet();
            _contextMock.Setup(x => x.Employees).Returns(employees.Object);

            // Enrollments rỗng
            _contextMock.Setup(x => x.Enrollments)
                .Returns(new List<Enrollment>().AsQueryable().BuildMockDbSet().Object);

            // Zoom
            _zoomServiceMock.Setup(x => x.CreateMeetingAsync(It.IsAny<ZoomMeetingRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ZoomMeetingResponse { JoinUrl = "https://zoom.us/j/123" });

            var command = new AssignEmployeesToCourseCommand
            {
                CourseId = courseId,
                EmployeeIds = new List<Guid> { employeeId }
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.TotalAssigned.Should().Be(1);

            _emailServiceMock.Verify(x =>
                x.SendEmailAsync(traineeEmail, It.Is<string>(s => s.Contains("[HỌC VIÊN]")), It.IsAny<string>()),
                Times.Once);

            _emailServiceMock.Verify(x =>
                x.SendEmailAsync(trainerEmail, It.Is<string>(s => s.Contains("[GIẢNG VIÊN]")), It.IsAny<string>()),
                Times.Once);

            _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_CourseNotFound_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(Guid.NewGuid());
            _contextMock.Setup(x => x.Courses).Returns(new List<Course>().AsQueryable().BuildMockDbSet().Object);

            var command = new AssignEmployeesToCourseCommand { CourseId = Guid.NewGuid() };

            // Act
            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task Handle_InvalidDepartmentEmployee_ShouldThrowException()
        {
            // Arrange (GIỮ NGUYÊN)

            var enterpriseId = Guid.NewGuid();
            var courseId = Guid.NewGuid();
            var trainingPlanId = Guid.NewGuid();
            var employeeId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            var courses = new List<Course>
    {
        new Course {
            Id = courseId,
            EnterpriseId = enterpriseId,
            TrainingPlanId = trainingPlanId,
            Status = "Published"
        }
    }.AsQueryable().BuildMockDbSet();
            _contextMock.Setup(x => x.Courses).Returns(courses.Object);

            var trainingRequests = new List<TrainingRequest>
    {
        new TrainingRequest {
            TrainingPlanId = trainingPlanId,
            DepartmentId = 1,
            IsDeleted = false
        }
    }.AsQueryable().BuildMockDbSet();
            _contextMock.Setup(x => x.TrainingRequests).Returns(trainingRequests.Object);

            var employees = new List<Employee>
    {
        new Employee {
            Id = employeeId,
            EnterpriseId = enterpriseId,
            DepartmentId = 2, // ❌ sai department
            User = new User { Email = "test@mail.com" }
        }
    }.AsQueryable().BuildMockDbSet();
            _contextMock.Setup(x => x.Employees).Returns(employees.Object);

            _contextMock.Setup(x => x.Enrollments)
                .Returns(new List<Enrollment>().AsQueryable().BuildMockDbSet().Object);

            var command = new AssignEmployeesToCourseCommand
            {
                CourseId = courseId,
                EmployeeIds = new List<Guid> { employeeId }
            };

            // Act
            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Có nhân viên không thuộc phòng ban của khóa học.");
        }

        [Fact]
        public async Task Handle_ExistingEnrollments_ShouldBeSkipped()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            var courseId = Guid.NewGuid();
            var trainingPlanId = Guid.NewGuid();
            var existingEmployeeId = Guid.NewGuid();
            var departmentId = 1;

            _currentUserServiceMock
                .Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            // Course (phải có TrainingPlanId)
            var courses = new List<Course>
    {
        new Course
        {
            Id = courseId,
            EnterpriseId = enterpriseId,
            TrainingPlanId = trainingPlanId,
            Status = "Published"
        }
    }.AsQueryable().BuildMockDbSet();
            _contextMock.Setup(x => x.Courses).Returns(courses.Object);

            // 🔥 TrainingRequest (bắt buộc)
            var trainingRequests = new List<TrainingRequest>
    {
        new TrainingRequest
        {
            TrainingPlanId = trainingPlanId,
            DepartmentId = departmentId,
            IsDeleted = false
        }
    }.AsQueryable().BuildMockDbSet();
            _contextMock.Setup(x => x.TrainingRequests).Returns(trainingRequests.Object);

            // Employee hợp lệ (đúng Department + Enterprise)
            var employees = new List<Employee>
    {
        new Employee
        {
            Id = existingEmployeeId,
            EnterpriseId = enterpriseId,
            DepartmentId = departmentId,
            User = new User { Email = "test@mail.com" }
        }
    }.AsQueryable().BuildMockDbSet();
            _contextMock.Setup(x => x.Employees).Returns(employees.Object);

            // Đã tồn tại enrollment
            var enrollments = new List<Enrollment>
    {
        new Enrollment
        {
            CourseId = courseId,
            EmployeeId = existingEmployeeId
        }
    }.AsQueryable().BuildMockDbSet();
            _contextMock.Setup(x => x.Enrollments).Returns(enrollments.Object);

            var command = new AssignEmployeesToCourseCommand
            {
                CourseId = courseId,
                EmployeeIds = new List<Guid> { existingEmployeeId }
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.TotalAssigned.Should().Be(0);
            result.SkippedEmployeeIds.Should().Contain(existingEmployeeId);

            // vẫn gửi mail (theo logic hiện tại của bạn)
            _emailServiceMock.Verify(x =>
                x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.AtLeastOnce);
        }
    }
}