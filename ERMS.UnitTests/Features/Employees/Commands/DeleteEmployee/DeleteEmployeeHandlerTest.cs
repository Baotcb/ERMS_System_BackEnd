using ERMS.Application.Features.Employees.Commands.DeleteEmployee;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Organization;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ERMS.UnitTests.Features.Employees.Commands.DeleteEmployee;

public class DeleteEmployeeHandlerTest
{
    private readonly Mock<IERMSDbContext> _mockContext;
    private readonly Mock<ILogger<DeleteEmployeeHandler>> _mockLogger;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<UserManager<User>> _mockUserManager;
    private readonly DeleteEmployeeHandler _handler;

    public DeleteEmployeeHandlerTest()
    {
        _mockContext = new Mock<IERMSDbContext>();
        _mockLogger = new Mock<ILogger<DeleteEmployeeHandler>>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockUserManager = MockUserManager();

        _handler = new DeleteEmployeeHandler(
            _mockContext.Object,
            _mockLogger.Object,
            _mockCurrentUserService.Object,
            _mockUserManager.Object
        );
    }

    [Fact]
    public async Task Handle_WhenUserNotBelongToEnterprise_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync((Guid?)null);

        var command = new DeleteEmployeeCommand { Id = Guid.NewGuid() };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Be("Người dùng không thuộc doanh nghiệp nào.");
    }

    [Fact]
    public async Task Handle_WhenEmployeeNotFound_ThrowsException()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        var employees = new List<Employee>().AsQueryable();
        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        var command = new DeleteEmployeeCommand { Id = Guid.NewGuid() };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Be("Nhân viên không tồn tại");
    }

    [Fact]
    public async Task Handle_WhenEmployeeFromDifferentEnterprise_ThrowsException()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var differentEnterpriseId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        var employee = new Employee
        {
            Id = employeeId,
            EnterpriseId = differentEnterpriseId,
            IsDeleted = false
        };

        var employees = new List<Employee> { employee }.AsQueryable();
        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        var command = new DeleteEmployeeCommand { Id = employeeId };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Be("Nhân viên không tồn tại");
    }

    [Fact]
    public async Task Handle_WhenEmployeeAlreadyDeleted_ThrowsException()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        var employee = new Employee
        {
            Id = employeeId,
            EnterpriseId = enterpriseId,
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow.AddDays(-1)
        };

        var employees = new List<Employee> { employee }.AsQueryable();
        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        var command = new DeleteEmployeeCommand { Id = employeeId };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Be("Nhân viên không tồn tại");
    }

    [Fact]
    public async Task Handle_WhenEmployeeExists_SoftDeletesEmployee()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        var employee = new Employee
        {
            Id = employeeId,
            UserId = userId,
            EnterpriseId = enterpriseId,
            IsDeleted = false,
            Status = "Active",
            DeletedAt = null
        };

        var employees = new List<Employee> { employee }.AsQueryable();
        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        var user = new User { Id = userId };
        _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _mockUserManager.Setup(x => x.SetLockoutEnabledAsync(user, true))
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager.Setup(x => x.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue))
            .ReturnsAsync(IdentityResult.Success);

        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new DeleteEmployeeCommand { Id = employeeId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        employee.IsDeleted.Should().BeTrue();
        employee.Status.Should().Be("Inactive");
        employee.DeletedAt.Should().NotBeNull();

        _mockUserManager.Verify(x => x.FindByIdAsync(userId.ToString()), Times.Once);
        _mockUserManager.Verify(x => x.SetLockoutEnabledAsync(user, true), Times.Once);
        _mockUserManager.Verify(x => x.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue), Times.Once);
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_StillDeletesEmployee()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        var employee = new Employee
        {
            Id = employeeId,
            UserId = userId,
            EnterpriseId = enterpriseId,
            IsDeleted = false,
            Status = "Active",
            DeletedAt = null
        };

        var employees = new List<Employee> { employee }.AsQueryable();
        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((User?)null);

        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new DeleteEmployeeCommand { Id = employeeId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        employee.IsDeleted.Should().BeTrue();
        employee.Status.Should().Be("Inactive");
        employee.DeletedAt.Should().NotBeNull();

        _mockUserManager.Verify(x => x.FindByIdAsync(userId.ToString()), Times.Once);
        _mockUserManager.Verify(x => x.SetLockoutEnabledAsync(It.IsAny<User>(), It.IsAny<bool>()), Times.Never);
        _mockUserManager.Verify(x => x.SetLockoutEndDateAsync(It.IsAny<User>(), It.IsAny<DateTimeOffset>()), Times.Never);
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDeleting_SetsCorrectDeletedAtTimestamp()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        var beforeDelete = DateTime.UtcNow;

        var employee = new Employee
        {
            Id = employeeId,
            UserId = userId,
            EnterpriseId = enterpriseId,
            IsDeleted = false,
            Status = "Active",
            DeletedAt = null
        };

        var employees = new List<Employee> { employee }.AsQueryable();
        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        _mockUserManager.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new DeleteEmployeeCommand { Id = employeeId };

        // Act
        await _handler.Handle(command, CancellationToken.None);
        var afterDelete = DateTime.UtcNow;

        // Assert
        employee.DeletedAt.Should().NotBeNull();
        employee.DeletedAt.Should().BeOnOrAfter(beforeDelete);
        employee.DeletedAt.Should().BeOnOrBefore(afterDelete);
    }

    [Fact]
    public async Task Handle_WhenDeleting_ChangesStatusToInactive()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        var employee = new Employee
        {
            Id = employeeId,
            UserId = userId,
            EnterpriseId = enterpriseId,
            IsDeleted = false,
            Status = "Active",
            DeletedAt = null
        };

        var employees = new List<Employee> { employee }.AsQueryable();
        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        _mockUserManager.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new DeleteEmployeeCommand { Id = employeeId };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        employee.Status.Should().Be("Inactive");
    }

    [Fact]
    public async Task Handle_WhenDeleting_LocksUserAccountPermanently()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        var employee = new Employee
        {
            Id = employeeId,
            UserId = userId,
            EnterpriseId = enterpriseId,
            IsDeleted = false,
            Status = "Active"
        };

        var employees = new List<Employee> { employee }.AsQueryable();
        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        var user = new User { Id = userId, Email = "test@example.com" };
        _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _mockUserManager.Setup(x => x.SetLockoutEnabledAsync(user, true))
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager.Setup(x => x.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue))
            .ReturnsAsync(IdentityResult.Success);

        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new DeleteEmployeeCommand { Id = employeeId };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockUserManager.Verify(x => x.SetLockoutEnabledAsync(user, true), Times.Once);
        _mockUserManager.Verify(x => x.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSuccessful_LogsInformation()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        var employee = new Employee
        {
            Id = employeeId,
            UserId = userId,
            EnterpriseId = enterpriseId,
            IsDeleted = false,
            Status = "Active"
        };

        var employees = new List<Employee> { employee }.AsQueryable();
        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        _mockUserManager.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new DeleteEmployeeCommand { Id = employeeId };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Deleted employee {employeeId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEmployeeHasOnLeaveStatus_StillChangesToInactive()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        var employee = new Employee
        {
            Id = employeeId,
            UserId = userId,
            EnterpriseId = enterpriseId,
            IsDeleted = false,
            Status = "OnLeave", // Different initial status
            DeletedAt = null
        };

        var employees = new List<Employee> { employee }.AsQueryable();
        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        _mockUserManager.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new DeleteEmployeeCommand { Id = employeeId };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        employee.Status.Should().Be("Inactive");
        employee.IsDeleted.Should().BeTrue();
    }

    // Helper Method
    private static Mock<UserManager<User>> MockUserManager()
    {
        var store = new Mock<IUserStore<User>>();
        return new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
    }
}