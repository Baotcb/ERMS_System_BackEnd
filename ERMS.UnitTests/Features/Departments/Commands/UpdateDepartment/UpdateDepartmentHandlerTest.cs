using ERMS.Application.Features.Departments.Commands.UpdateDepartment;
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

namespace ERMS.UnitTests.Features.Departments.Commands.UpdateDepartment
{
    public class UpdateDepartmentHandlerTest
    {
        private readonly Mock<IERMSDbContext> _contextMock;
        private readonly Mock<ILogger<UpdateDepartmentHandler>> _loggerMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly UpdateDepartmentHandler _handler;

        public UpdateDepartmentHandlerTest()
        {
            _contextMock = new Mock<IERMSDbContext>();
            _loggerMock = new Mock<ILogger<UpdateDepartmentHandler>>();
            _currentUserServiceMock = new Mock<ICurrentUserService>();

            _handler = new UpdateDepartmentHandler(_contextMock.Object, _loggerMock.Object, _currentUserServiceMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserNotInEnterprise()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync((Guid?)null);
            var command = new UpdateDepartmentCommand();

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Người dùng không thuộc doanh nghiệp nào.");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenDepartmentNotFound()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var departments = new List<Department>().AsQueryable().BuildMockDbSet();
            _contextMock.Setup(c => c.Departments).Returns(departments.Object);

            var command = new UpdateDepartmentCommand { Id = 1 };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Phòng ban không tồn tại");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenDepartmentCodeExists()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var department = new Department { Id = 1, EnterpriseId = enterpriseId, IsDeleted = false };
            var existingOtherDepartment = new Department { Id = 2, EnterpriseId = enterpriseId, DepartmentCode = "HR", IsDeleted = false };
            
            var departments = new List<Department> { department, existingOtherDepartment }.AsQueryable().BuildMockDbSet();
            _contextMock.Setup(c => c.Departments).Returns(departments.Object);

            var command = new UpdateDepartmentCommand { Id = 1, DepartmentCode = "HR" };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Mã phòng ban đã tồn tại trong doanh nghiệp");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenSelfAssignedAsParent()
        {
             // Arrange
            var enterpriseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var department = new Department { Id = 1, EnterpriseId = enterpriseId, IsDeleted = false };
            
            var departments = new List<Department> { department }.AsQueryable().BuildMockDbSet();
            _contextMock.Setup(c => c.Departments).Returns(departments.Object);

            var command = new UpdateDepartmentCommand { Id = 1, ParentDepartmentId = 1 };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Phòng ban không thể là cha của chính nó");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenParentDepartmentNotFound()
        {
             // Arrange
            var enterpriseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var department = new Department { Id = 1, EnterpriseId = enterpriseId, IsDeleted = false };
            
            var departments = new List<Department> { department }.AsQueryable().BuildMockDbSet();
            _contextMock.Setup(c => c.Departments).Returns(departments.Object);

            var command = new UpdateDepartmentCommand { Id = 1, ParentDepartmentId = 999 };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Phòng ban cha không tồn tại");
        }

        [Fact]
        public async Task Handle_ShouldUpdateDepartment_WhenValidRequest()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var department = new Department 
            { 
                Id = 1, 
                EnterpriseId = enterpriseId, 
                DepartmentName = "Old Name",
                IsDeleted = false 
            };
            
            var departments = new List<Department> { department }.AsQueryable().BuildMockDbSet();
            _contextMock.Setup(c => c.Departments).Returns(departments.Object);

            var command = new UpdateDepartmentCommand 
            { 
                Id = 1,
                DepartmentName = "New Name", 
                DepartmentCode = "NEW", 
                Description = "New Desc",
                IsActive = false
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            department.DepartmentName.Should().Be("New Name");
            department.DepartmentCode.Should().Be("NEW");
            department.Description.Should().Be("New Desc");
            department.IsActive.Should().BeFalse();

            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
