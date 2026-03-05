using ERMS.Application.Features.Departments.Commands.DeleteDepartment;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Organization;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Departments.Commands.DeleteDepartment
{
    public class DeleteDepartmentHandlerTest
    {
        private readonly Mock<IERMSDbContext> _contextMock;
        private readonly Mock<ILogger<DeleteDepartmentHandler>> _loggerMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly DeleteDepartmentHandler _handler;

        public DeleteDepartmentHandlerTest()
        {
            _contextMock = new Mock<IERMSDbContext>();
            _loggerMock = new Mock<ILogger<DeleteDepartmentHandler>>();
            _currentUserServiceMock = new Mock<ICurrentUserService>();

            _handler = new DeleteDepartmentHandler(_contextMock.Object, _loggerMock.Object, _currentUserServiceMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserNotInEnterprise()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync((Guid?)null);
            var command = new DeleteDepartmentCommand { Id = 1 };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("User not belong to enterprise");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenDepartmentNotFound()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var departments = new List<Department>().AsQueryable().BuildMockDbSet();
            _contextMock.Setup(c => c.Departments).Returns(departments.Object);

            var command = new DeleteDepartmentCommand { Id = 1 };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Phòng ban không tồn tại");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenDepartmentHasEmployees()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var department = new Department { Id = 1, EnterpriseId = enterpriseId, IsDeleted = false };
            var departments = new List<Department> { department }.AsQueryable().BuildMockDbSet();
            _contextMock.Setup(c => c.Departments).Returns(departments.Object);

            var employee = new Employee { DepartmentId = 1, IsDeleted = false };
            var employees = new List<Employee> { employee }.AsQueryable().BuildMockDbSet();
            _contextMock.Setup(c => c.Employees).Returns(employees.Object);

            var command = new DeleteDepartmentCommand { Id = 1 };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Không thể xóa phòng ban đang có nhân viên");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenDepartmentHasChildren()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var department = new Department { Id = 1, EnterpriseId = enterpriseId, IsDeleted = false };
            var childDepartment = new Department { Id = 2, EnterpriseId = enterpriseId, ParentDepartmentId = 1, IsDeleted = false };
            
            var departments = new List<Department> { department, childDepartment }.AsQueryable().BuildMockDbSet();
            _contextMock.Setup(c => c.Departments).Returns(departments.Object);

            var employees = new List<Employee>().AsQueryable().BuildMockDbSet();
            _contextMock.Setup(c => c.Employees).Returns(employees.Object);

            var command = new DeleteDepartmentCommand { Id = 1 };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Không thể xóa phòng ban đang có phòng ban con");
        }

        [Fact]
        public async Task Handle_ShouldSoftDeleteDepartment_WhenValidRequest()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var department = new Department 
            { 
                Id = 1, 
                EnterpriseId = enterpriseId, 
                IsActive = true,
                IsDeleted = false 
            };
            
            var departments = new List<Department> { department }.AsQueryable().BuildMockDbSet();
            _contextMock.Setup(c => c.Departments).Returns(departments.Object);

            var employees = new List<Employee>().AsQueryable().BuildMockDbSet();
            _contextMock.Setup(c => c.Employees).Returns(employees.Object);

            var command = new DeleteDepartmentCommand { Id = 1 };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            department.IsDeleted.Should().BeTrue();
            department.IsActive.Should().BeFalse();
            department.DeletedAt.Should().NotBeNull();

            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
