using ERMS.Application.Features.Departments.Commands.CreateDepartment;
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

namespace ERMS.UnitTests.Features.Departments.Commands.CreateDepartment
{
    public class CreateDepartmentHandlerTest
    {
        private readonly Mock<IERMSDbContext> _contextMock;
        private readonly Mock<ILogger<CreateDepartmentHandler>> _loggerMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly CreateDepartmentHandler _handler;

        public CreateDepartmentHandlerTest()
        {
            _contextMock = new Mock<IERMSDbContext>();
            _loggerMock = new Mock<ILogger<CreateDepartmentHandler>>();
            _currentUserServiceMock = new Mock<ICurrentUserService>();

            _handler = new CreateDepartmentHandler(_contextMock.Object, _loggerMock.Object, _currentUserServiceMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserNotInEnterprise()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync((Guid?)null);
            var command = new CreateDepartmentCommand();

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("User not belong to enterprise");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenEnterpriseNotFound()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var enterprises = new List<ERMS.Domain.Entities.Enterprise.Enterprise>().AsQueryable().BuildMockDbSet();
            _contextMock.Setup(c => c.Enterprises).Returns(enterprises.Object);

            var command = new CreateDepartmentCommand();

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Doanh nghiệp không tồn tại");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenDepartmentCodeExists()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var existingEnterprise = new ERMS.Domain.Entities.Enterprise.Enterprise { Id = enterpriseId, IsDeleted = false };
            var enterprises = new List<ERMS.Domain.Entities.Enterprise.Enterprise> { existingEnterprise }.AsQueryable().BuildMockDbSet();
            _contextMock.Setup(c => c.Enterprises).Returns(enterprises.Object);

            var existingDepartment = new Department { EnterpriseId = enterpriseId, DepartmentCode = "HR", IsDeleted = false };
            var departments = new List<Department> { existingDepartment }.AsQueryable().BuildMockDbSet();
            _contextMock.Setup(c => c.Departments).Returns(departments.Object);

            var command = new CreateDepartmentCommand { DepartmentCode = "HR" };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Mã phòng ban đã tồn tại trong doanh nghiệp");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenParentDepartmentNotFound()
        {
             // Arrange
            var enterpriseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var existingEnterprise = new ERMS.Domain.Entities.Enterprise.Enterprise { Id = enterpriseId, IsDeleted = false };
            var enterprises = new List<ERMS.Domain.Entities.Enterprise.Enterprise> { existingEnterprise }.AsQueryable().BuildMockDbSet();
            _contextMock.Setup(c => c.Enterprises).Returns(enterprises.Object);

            var departments = new List<Department>().AsQueryable().BuildMockDbSet();
            _contextMock.Setup(c => c.Departments).Returns(departments.Object);

            var command = new CreateDepartmentCommand { ParentDepartmentId = 999 };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Phòng ban cha không tồn tại");
        }

        [Fact]
        public async Task Handle_ShouldCreateDepartment_WhenValidRequest()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var existingEnterprise = new ERMS.Domain.Entities.Enterprise.Enterprise { Id = enterpriseId, IsDeleted = false };
            var enterprises = new List<ERMS.Domain.Entities.Enterprise.Enterprise> { existingEnterprise }.AsQueryable().BuildMockDbSet();
            _contextMock.Setup(c => c.Enterprises).Returns(enterprises.Object);

            var departments = new List<Department>().AsQueryable().BuildMockDbSet();
            
            Department addedDepartment = null;
            departments.Setup(d => d.Add(It.IsAny<Department>())).Callback<Department>(d => addedDepartment = d);

            _contextMock.Setup(c => c.Departments).Returns(departments.Object);

            var command = new CreateDepartmentCommand 
            { 
                DepartmentName = "IT Department", 
                DepartmentCode = "IT", 
                Description = "Information Tech"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            addedDepartment.Should().NotBeNull();
            addedDepartment.EnterpriseId.Should().Be(enterpriseId);
            addedDepartment.DepartmentName.Should().Be("IT Department");
            addedDepartment.DepartmentCode.Should().Be("IT");
            addedDepartment.IsActive.Should().BeTrue();
            addedDepartment.IsDeleted.Should().BeFalse();

            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
