using ERMS.Application.Features.Employees.Commands.UpdateEmployee;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Organization;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ERMS.UnitTests.Features.Employees.Commands.UpdateEmployee;

public class UpdateEmployeeHandlerTest
{
    private readonly Mock<IERMSDbContext> _mockContext;
    private readonly Mock<ILogger<UpdateEmployeeHandler>> _mockLogger;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<UserManager<User>> _mockUserManager;
    private readonly UpdateEmployeeHandler _handler;

    public UpdateEmployeeHandlerTest()
    {
        _mockContext = new Mock<IERMSDbContext>();
        _mockLogger = new Mock<ILogger<UpdateEmployeeHandler>>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockUserManager = MockUserManager();

        // Mock transaction cho BeginTransactionAsync
        var transaction = new Mock<IDbContextTransaction>();
        transaction.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        transaction.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        transaction.Setup(x => x.DisposeAsync())
            .Returns(ValueTask.CompletedTask);

        _mockContext.Setup(c => c.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);

        _handler = new UpdateEmployeeHandler(
            _mockContext.Object,
            _mockLogger.Object,
            _mockCurrentUserService.Object,
            _mockUserManager.Object
        );
    }

    private static Mock<UserManager<User>> MockUserManager()
    {
        var store = new Mock<IUserStore<User>>();
        return new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
    }

    [Fact]
    public async Task Handle_WhenUserNotBelongToEnterprise_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync((Guid?)null);

        var command = new UpdateEmployeeCommand { Id = Guid.NewGuid() };

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

        var command = new UpdateEmployeeCommand { Id = Guid.NewGuid() };

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

        var command = new UpdateEmployeeCommand { Id = employeeId };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Be("Nhân viên không tồn tại");
    }

    [Fact]
    public async Task Handle_WhenEmployeeIsDeleted_ThrowsException()
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
            IsDeleted = true
        };

        var employees = new List<Employee> { employee }.AsQueryable();
        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        var command = new UpdateEmployeeCommand { Id = employeeId };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Be("Nhân viên không tồn tại");
    }

    [Fact]
    public async Task Handle_WhenDepartmentNotFound_ThrowsException()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var departmentId = 1;

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        var employee = new Employee
        {
            Id = employeeId,
            EnterpriseId = enterpriseId,
            DepartmentId = 999,
            IsDeleted = false
        };

        var employees = new List<Employee> { employee }.AsQueryable();
        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        var departments = new List<Department>().AsQueryable();
        _mockContext.Setup(x => x.Departments)
            .Returns(DbContextMockHelper.BuildMockDbSet(departments).Object);

        var command = new UpdateEmployeeCommand
        {
            Id = employeeId,
            DepartmentId = departmentId
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Be("Phòng ban không tồn tại");
    }

    [Fact]
    public async Task Handle_WhenDepartmentFromDifferentEnterprise_ThrowsException()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var differentEnterpriseId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var departmentId = 1;

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        var employee = new Employee
        {
            Id = employeeId,
            EnterpriseId = enterpriseId,
            DepartmentId = 999,
            IsDeleted = false
        };

        var employees = new List<Employee> { employee }.AsQueryable();
        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        var department = new Department
        {
            Id = departmentId,
            EnterpriseId = differentEnterpriseId,
            IsDeleted = false
        };

        var departments = new List<Department> { department }.AsQueryable();
        _mockContext.Setup(x => x.Departments)
            .Returns(DbContextMockHelper.BuildMockDbSet(departments).Object);

        var command = new UpdateEmployeeCommand
        {
            Id = employeeId,
            DepartmentId = departmentId
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Be("Phòng ban không tồn tại");
    }

    [Fact]
    public async Task Handle_WhenDepartmentIsDeleted_ThrowsException()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var departmentId = 1;

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        var employee = new Employee
        {
            Id = employeeId,
            EnterpriseId = enterpriseId,
            DepartmentId = 999,
            IsDeleted = false
        };

        var employees = new List<Employee> { employee }.AsQueryable();
        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        var department = new Department
        {
            Id = departmentId,
            EnterpriseId = enterpriseId,
            IsDeleted = true
        };

        var departments = new List<Department> { department }.AsQueryable();
        _mockContext.Setup(x => x.Departments)
            .Returns(DbContextMockHelper.BuildMockDbSet(departments).Object);

        var command = new UpdateEmployeeCommand
        {
            Id = employeeId,
            DepartmentId = departmentId
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Be("Phòng ban không tồn tại");
    }

    [Fact]
    public async Task Handle_WhenValidData_UpdatesEmployeeSuccessfully()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var oldDepartmentId = 1;
        var newDepartmentId = 2;
        var managerId = Guid.NewGuid();

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        var employee = new Employee
        {
            Id = employeeId,
            EnterpriseId = enterpriseId,
            DepartmentId = oldDepartmentId,
            Position = "Old Position",
            EmploymentType = "PartTime",
            Status = "OnLeave",
            IsDeleted = false,
            UpdatedAt = null
        };

        var managerEmployee = new Employee
        {
            Id = managerId,
            EnterpriseId = enterpriseId,
            IsDeleted = false
        };

        var employees = new List<Employee> { employee, managerEmployee }.AsQueryable();
        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        var department = new Department
        {
            Id = newDepartmentId,
            EnterpriseId = enterpriseId,
            IsDeleted = false
        };

        var departments = new List<Department> { department }.AsQueryable();
        _mockContext.Setup(x => x.Departments)
            .Returns(DbContextMockHelper.BuildMockDbSet(departments).Object);

        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateEmployeeCommand
        {
            Id = employeeId,
            DepartmentId = newDepartmentId,
            Position = "New Position",
            EmploymentType = "FullTime",
            ManagerId = managerId,
            Status = "Active"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        employee.DepartmentId.Should().Be(newDepartmentId);
        employee.Position.Should().Be("New Position");
        employee.EmploymentType.Should().Be("FullTime");
        employee.ManagerId.Should().Be(managerId);
        employee.Status.Should().Be("Active");
        employee.UpdatedAt.Should().NotBeNull();

        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUpdating_SetsUpdatedAtTimestamp()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var departmentId = 1;

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        var beforeUpdate = DateTime.UtcNow;

        var employee = new Employee
        {
            Id = employeeId,
            EnterpriseId = enterpriseId,
            DepartmentId = departmentId,
            IsDeleted = false,
            UpdatedAt = null
        };

        var employees = new List<Employee> { employee }.AsQueryable();
        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        var department = new Department
        {
            Id = departmentId,
            EnterpriseId = enterpriseId,
            IsDeleted = false
        };

        var departments = new List<Department> { department }.AsQueryable();
        _mockContext.Setup(x => x.Departments)
            .Returns(DbContextMockHelper.BuildMockDbSet(departments).Object);

        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateEmployeeCommand
        {
            Id = employeeId,
            DepartmentId = departmentId,
            Position = "Position",
            EmploymentType = "FullTime",
            Status = "Active"
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);
        var afterUpdate = DateTime.UtcNow;

        // Assert
        employee.UpdatedAt.Should().NotBeNull();
        employee.UpdatedAt.Should().BeOnOrAfter(beforeUpdate);
        employee.UpdatedAt.Should().BeOnOrBefore(afterUpdate);
    }

    [Fact]
    public async Task Handle_WhenUpdating_UpdatesAllFields()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var departmentId = 1;
        var managerId = Guid.NewGuid();

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        var employee = new Employee
        {
            Id = employeeId,
            EnterpriseId = enterpriseId,
            DepartmentId = 999,
            Position = "Old Position",
            EmploymentType = "PartTime",
            ManagerId = null,
            Status = "OnLeave",
            IsDeleted = false
        };

        var managerEmployee = new Employee
        {
            Id = managerId,
            EnterpriseId = enterpriseId,
            IsDeleted = false
        };

        var employees = new List<Employee> { employee, managerEmployee }.AsQueryable();
        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        var department = new Department
        {
            Id = departmentId,
            EnterpriseId = enterpriseId,
            IsDeleted = false
        };

        var departments = new List<Department> { department }.AsQueryable();
        _mockContext.Setup(x => x.Departments)
            .Returns(DbContextMockHelper.BuildMockDbSet(departments).Object);

        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateEmployeeCommand
        {
            Id = employeeId,
            DepartmentId = departmentId,
            Position = "Senior Developer",
            EmploymentType = "Contract",
            ManagerId = managerId,
            Status = "Active"
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        employee.DepartmentId.Should().Be(departmentId);
        employee.Position.Should().Be("Senior Developer");
        employee.EmploymentType.Should().Be("Contract");
        employee.ManagerId.Should().Be(managerId);
        employee.Status.Should().Be("Active");
    }

    [Fact]
    public async Task Handle_WhenManagerIdIsNull_UpdatesManagerIdToNull()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var departmentId = 1;
        var oldManagerId = Guid.NewGuid();

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        var employee = new Employee
        {
            Id = employeeId,
            EnterpriseId = enterpriseId,
            DepartmentId = departmentId,
            ManagerId = oldManagerId,
            IsDeleted = false
        };

        var employees = new List<Employee> { employee }.AsQueryable();
        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        var department = new Department
        {
            Id = departmentId,
            EnterpriseId = enterpriseId,
            IsDeleted = false
        };

        var departments = new List<Department> { department }.AsQueryable();
        _mockContext.Setup(x => x.Departments)
            .Returns(DbContextMockHelper.BuildMockDbSet(departments).Object);

        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateEmployeeCommand
        {
            Id = employeeId,
            DepartmentId = departmentId,
            Position = "Developer",
            EmploymentType = "FullTime",
            ManagerId = null,
            Status = "Active"
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        employee.ManagerId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenPositionIsNull_UpdatesPositionToNull()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var departmentId = 1;

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        var employee = new Employee
        {
            Id = employeeId,
            EnterpriseId = enterpriseId,
            DepartmentId = departmentId,
            Position = "Old Position",
            IsDeleted = false
        };

        var employees = new List<Employee> { employee }.AsQueryable();
        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        var department = new Department
        {
            Id = departmentId,
            EnterpriseId = enterpriseId,
            IsDeleted = false
        };

        var departments = new List<Department> { department }.AsQueryable();
        _mockContext.Setup(x => x.Departments)
            .Returns(DbContextMockHelper.BuildMockDbSet(departments).Object);

        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateEmployeeCommand
        {
            Id = employeeId,
            DepartmentId = departmentId,
            Position = null,
            EmploymentType = "FullTime",
            Status = "Active"
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        employee.Position.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenSuccessful_LogsInformation()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var departmentId = 1;

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        var employee = new Employee
        {
            Id = employeeId,
            EnterpriseId = enterpriseId,
            DepartmentId = departmentId,
            IsDeleted = false
        };

        var employees = new List<Employee> { employee }.AsQueryable();
        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        var department = new Department
        {
            Id = departmentId,
            EnterpriseId = enterpriseId,
            IsDeleted = false
        };

        var departments = new List<Department> { department }.AsQueryable();
        _mockContext.Setup(x => x.Departments)
            .Returns(DbContextMockHelper.BuildMockDbSet(departments).Object);

        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateEmployeeCommand
        {
            Id = employeeId,
            DepartmentId = departmentId,
            Position = "Developer",
            EmploymentType = "FullTime",
            Status = "Active"
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Updated employee {employeeId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUpdatingSameDepartment_Succeeds()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var departmentId = 1;

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        var employee = new Employee
        {
            Id = employeeId,
            EnterpriseId = enterpriseId,
            DepartmentId = departmentId,
            Position = "Developer",
            IsDeleted = false
        };

        var employees = new List<Employee> { employee }.AsQueryable();
        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        var department = new Department
        {
            Id = departmentId,
            EnterpriseId = enterpriseId,
            IsDeleted = false
        };

        var departments = new List<Department> { department }.AsQueryable();
        _mockContext.Setup(x => x.Departments)
            .Returns(DbContextMockHelper.BuildMockDbSet(departments).Object);

        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateEmployeeCommand
        {
            Id = employeeId,
            DepartmentId = departmentId, 
            Position = "Senior Developer",
            EmploymentType = "FullTime",
            Status = "Active"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        employee.Position.Should().Be("Senior Developer");
    }

    [Fact]
    public async Task Handle_WhenUpdatingMultipleTimes_UpdatesUpdatedAtEachTime()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var departmentId = 1;

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        var employee = new Employee
        {
            Id = employeeId,
            EnterpriseId = enterpriseId,
            DepartmentId = departmentId,
            IsDeleted = false,
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var employees = new List<Employee> { employee }.AsQueryable();
        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        var department = new Department
        {
            Id = departmentId,
            EnterpriseId = enterpriseId,
            IsDeleted = false
        };

        var departments = new List<Department> { department }.AsQueryable();
        _mockContext.Setup(x => x.Departments)
            .Returns(DbContextMockHelper.BuildMockDbSet(departments).Object);

        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var oldUpdatedAt = employee.UpdatedAt;
        var command = new UpdateEmployeeCommand
        {
            Id = employeeId,
            DepartmentId = departmentId,
            Position = "Updated Position",
            EmploymentType = "FullTime",
            Status = "Active"
        };

     
        await Task.Delay(10);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        employee.UpdatedAt.Should().NotBe(oldUpdatedAt);
        employee.UpdatedAt.Should().BeAfter(oldUpdatedAt!.Value);
    }

    [Fact]
    public async Task Handle_WhenEmploymentTypeInvalid_ThrowsException()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        var command = new UpdateEmployeeCommand
        {
            Id = Guid.NewGuid(),
            DepartmentId = 1,
            EmploymentType = "InvalidType",
            Status = "Active"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("Loại hợp đồng không hợp lệ");
    }

    [Fact]
    public async Task Handle_WhenEmploymentTypeUsesLegacyFullTimeAlias_NormalizesAndUpdatesSuccessfully()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var departmentId = 1;

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        var employee = new Employee
        {
            Id = employeeId,
            EnterpriseId = enterpriseId,
            DepartmentId = departmentId,
            EmploymentType = "Full-time",
            IsDeleted = false
        };

        var employees = new List<Employee> { employee }.AsQueryable();
        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        var department = new Department
        {
            Id = departmentId,
            EnterpriseId = enterpriseId,
            IsDeleted = false
        };

        var departments = new List<Department> { department }.AsQueryable();
        _mockContext.Setup(x => x.Departments)
            .Returns(DbContextMockHelper.BuildMockDbSet(departments).Object);

        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateEmployeeCommand
        {
            Id = employeeId,
            DepartmentId = departmentId,
            Position = "Developer",
            EmploymentType = "Full-time",
            Status = "Active"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        employee.EmploymentType.Should().Be("FullTime");
    }

    [Fact]
    public async Task Handle_WhenStatusInvalid_ThrowsException()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        var command = new UpdateEmployeeCommand
        {
            Id = Guid.NewGuid(),
            DepartmentId = 1,
            EmploymentType = "FullTime",
            Status = "InvalidStatus"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("Trạng thái không hợp lệ");
    }

    [Fact]
    public async Task Handle_WhenManagerIdIsSelf_ThrowsException()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var departmentId = 1;

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        var employee = new Employee
        {
            Id = employeeId,
            EnterpriseId = enterpriseId,
            DepartmentId = departmentId,
            IsDeleted = false
        };

        var employees = new List<Employee> { employee }.AsQueryable();
        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        var department = new Department
        {
            Id = departmentId,
            EnterpriseId = enterpriseId,
            IsDeleted = false
        };

        var departments = new List<Department> { department }.AsQueryable();
        _mockContext.Setup(x => x.Departments)
            .Returns(DbContextMockHelper.BuildMockDbSet(departments).Object);

        var command = new UpdateEmployeeCommand
        {
            Id = employeeId,
            DepartmentId = departmentId,
            Position = "Developer",
            EmploymentType = "FullTime",
            ManagerId = employeeId, // Tự quản lý chính mình
            Status = "Active"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Be("Nhân viên không thể tự quản lý chính mình");
    }

    [Fact]
    public async Task Handle_WhenManagerIdNotExists_ThrowsException()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var nonExistentManagerId = Guid.NewGuid();
        var departmentId = 1;

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        var employee = new Employee
        {
            Id = employeeId,
            EnterpriseId = enterpriseId,
            DepartmentId = departmentId,
            ManagerId = null, // Hiện tại không có manager
            IsDeleted = false
        };

        var employees = new List<Employee> { employee }.AsQueryable();
        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        var department = new Department
        {
            Id = departmentId,
            EnterpriseId = enterpriseId,
            IsDeleted = false
        };

        var departments = new List<Department> { department }.AsQueryable();
        _mockContext.Setup(x => x.Departments)
            .Returns(DbContextMockHelper.BuildMockDbSet(departments).Object);

        var command = new UpdateEmployeeCommand
        {
            Id = employeeId,
            DepartmentId = departmentId,
            Position = "Developer",
            EmploymentType = "FullTime",
            ManagerId = nonExistentManagerId, // Gán manager mới nhưng không tồn tại
            Status = "Active"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Be("Quản lý trực tiếp không tồn tại");
    }

    [Fact]
    public async Task Handle_WhenManagerIdFromDifferentEnterprise_ThrowsException()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var differentEnterpriseId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var departmentId = 1;

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        var employee = new Employee
        {
            Id = employeeId,
            EnterpriseId = enterpriseId,
            DepartmentId = departmentId,
            IsDeleted = false
        };

        var manager = new Employee
        {
            Id = managerId,
            EnterpriseId = differentEnterpriseId, // Khác enterprise
            IsDeleted = false
        };

        var employees = new List<Employee> { employee, manager }.AsQueryable();
        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        var department = new Department
        {
            Id = departmentId,
            EnterpriseId = enterpriseId,
            IsDeleted = false
        };

        var departments = new List<Department> { department }.AsQueryable();
        _mockContext.Setup(x => x.Departments)
            .Returns(DbContextMockHelper.BuildMockDbSet(departments).Object);

        var command = new UpdateEmployeeCommand
        {
            Id = employeeId,
            DepartmentId = departmentId,
            Position = "Developer",
            EmploymentType = "FullTime",
            ManagerId = managerId,
            Status = "Active"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Be("Quản lý trực tiếp không tồn tại");
    }

    [Theory]
    [InlineData("active", "Active")]
    [InlineData("inactive", "Inactive")]
    [InlineData("onleave", "OnLeave")]
    [InlineData("TERMINATED", "Terminated")]
    [InlineData("On Leave", "OnLeave")]
    [InlineData("On-leave", "OnLeave")]
    public async Task Handle_WhenStatusHasDifferentCase_NormalizesBeforeSaving(string inputStatus, string expectedStatus)
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var departmentId = 1;

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        var employee = new Employee
        {
            Id = employeeId,
            EnterpriseId = enterpriseId,
            DepartmentId = departmentId,
            Status = "Active",
            IsDeleted = false
        };

        var employees = new List<Employee> { employee }.AsQueryable();
        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        var department = new Department
        {
            Id = departmentId,
            EnterpriseId = enterpriseId,
            IsDeleted = false
        };

        var departments = new List<Department> { department }.AsQueryable();
        _mockContext.Setup(x => x.Departments)
            .Returns(DbContextMockHelper.BuildMockDbSet(departments).Object);

        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateEmployeeCommand
        {
            Id = employeeId,
            DepartmentId = departmentId,
            Position = "Developer",
            EmploymentType = "FullTime",
            Status = inputStatus
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - Status phải được normalize về canonical form
        employee.Status.Should().Be(expectedStatus);
    }

    [Fact]
    public async Task Handle_WhenManagerIsSoftDeleted_AndManagerIdUnchanged_Succeeds()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var deletedManagerId = Guid.NewGuid();
        var departmentId = 1;

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        var employee = new Employee
        {
            Id = employeeId,
            EnterpriseId = enterpriseId,
            DepartmentId = departmentId,
            ManagerId = deletedManagerId, // Manager hiện tại đã bị soft-delete
            Position = "Developer",
            IsDeleted = false
        };

        var deletedManager = new Employee
        {
            Id = deletedManagerId,
            EnterpriseId = enterpriseId,
            IsDeleted = true // Đã bị soft-delete
        };

        var employees = new List<Employee> { employee, deletedManager }.AsQueryable();
        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        var department = new Department
        {
            Id = departmentId,
            EnterpriseId = enterpriseId,
            IsDeleted = false
        };

        var departments = new List<Department> { department }.AsQueryable();
        _mockContext.Setup(x => x.Departments)
            .Returns(DbContextMockHelper.BuildMockDbSet(departments).Object);

        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateEmployeeCommand
        {
            Id = employeeId,
            DepartmentId = departmentId,
            Position = "Senior Developer", // Chỉ thay đổi position
            EmploymentType = "FullTime",
            ManagerId = deletedManagerId, // Giữ nguyên ManagerId
            Status = "Active"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - Không throw exception, update thành công
        result.Should().BeTrue();
        employee.Position.Should().Be("Senior Developer");
        employee.ManagerId.Should().Be(deletedManagerId);
    }

    [Fact]
    public async Task Handle_WhenReassigningToSoftDeletedManager_ThrowsException()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var deletedManagerId = Guid.NewGuid();
        var departmentId = 1;

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        var employee = new Employee
        {
            Id = employeeId,
            EnterpriseId = enterpriseId,
            DepartmentId = departmentId,
            ManagerId = null, // Hiện tại không có manager
            IsDeleted = false
        };

        var deletedManager = new Employee
        {
            Id = deletedManagerId,
            EnterpriseId = enterpriseId,
            IsDeleted = true // Đã bị soft-delete
        };

        var employees = new List<Employee> { employee, deletedManager }.AsQueryable();
        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        var department = new Department
        {
            Id = departmentId,
            EnterpriseId = enterpriseId,
            IsDeleted = false
        };

        var departments = new List<Department> { department }.AsQueryable();
        _mockContext.Setup(x => x.Departments)
            .Returns(DbContextMockHelper.BuildMockDbSet(departments).Object);

        var command = new UpdateEmployeeCommand
        {
            Id = employeeId,
            DepartmentId = departmentId,
            Position = "Developer",
            EmploymentType = "FullTime",
            ManagerId = deletedManagerId, // Gán manager mới nhưng đã bị xóa
            Status = "Active"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Be("Quản lý trực tiếp không tồn tại");
    }

    [Theory]
    [InlineData(AppRoles.Director)]
    [InlineData(AppRoles.HRManager)]
    public async Task Handle_WhenKeepingExistingPrivilegedRole_Succeeds(string existingRole)
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var departmentId = 1;

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        var employee = new Employee
        {
            Id = employeeId,
            UserId = userId,
            EnterpriseId = enterpriseId,
            DepartmentId = departmentId,
            IsDeleted = false
        };

        var employees = new List<Employee> { employee }.AsQueryable();
        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        var department = new Department
        {
            Id = departmentId,
            EnterpriseId = enterpriseId,
            IsDeleted = false
        };

        var departments = new List<Department> { department }.AsQueryable();
        _mockContext.Setup(x => x.Departments)
            .Returns(DbContextMockHelper.BuildMockDbSet(departments).Object);

        var user = new User { Id = userId };
        _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { existingRole });
        _mockUserManager.Setup(x => x.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(x => x.AddToRoleAsync(user, existingRole))
            .ReturnsAsync(IdentityResult.Success);

        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateEmployeeCommand
        {
            Id = employeeId,
            DepartmentId = departmentId,
            Position = "Updated Position",
            EmploymentType = "FullTime",
            Status = "Active",
            Role = existingRole
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        employee.Position.Should().Be("Updated Position");
        _mockUserManager.Verify(x => x.AddToRoleAsync(user, existingRole), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenHrManagerDemotesDirector_ThrowsException()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var departmentId = 1;

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);
        _mockCurrentUserService.SetupGet(x => x.Roles)
            .Returns(new[] { AppRoles.HRManager });

        var employee = new Employee
        {
            Id = employeeId,
            UserId = userId,
            EnterpriseId = enterpriseId,
            DepartmentId = departmentId,
            IsDeleted = false
        };

        var employees = new List<Employee> { employee }.AsQueryable();
        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        var department = new Department
        {
            Id = departmentId,
            EnterpriseId = enterpriseId,
            IsDeleted = false
        };

        var departments = new List<Department> { department }.AsQueryable();
        _mockContext.Setup(x => x.Departments)
            .Returns(DbContextMockHelper.BuildMockDbSet(departments).Object);

        var user = new User { Id = userId };
        _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { AppRoles.Director, AppRoles.Employee });

        var command = new UpdateEmployeeCommand
        {
            Id = employeeId,
            DepartmentId = departmentId,
            Position = "Developer",
            EmploymentType = "FullTime",
            Status = "Active",
            Role = AppRoles.Employee
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Be("Chỉ Director mới có thể thay đổi vai trò của HR Manager hoặc Director.");
        _mockUserManager.Verify(x => x.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRemovingLastDirector_ThrowsException()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var otherEmployeeId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var departmentId = 1;

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);
        _mockCurrentUserService.SetupGet(x => x.Roles)
            .Returns(new[] { AppRoles.Director });

        var employee = new Employee
        {
            Id = employeeId,
            UserId = userId,
            EnterpriseId = enterpriseId,
            DepartmentId = departmentId,
            IsDeleted = false
        };

        var otherEmployee = new Employee
        {
            Id = otherEmployeeId,
            UserId = otherUserId,
            EnterpriseId = enterpriseId,
            DepartmentId = departmentId,
            IsDeleted = false
        };

        var employees = new List<Employee> { employee, otherEmployee }.AsQueryable();
        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        var department = new Department
        {
            Id = departmentId,
            EnterpriseId = enterpriseId,
            IsDeleted = false
        };

        var departments = new List<Department> { department }.AsQueryable();
        _mockContext.Setup(x => x.Departments)
            .Returns(DbContextMockHelper.BuildMockDbSet(departments).Object);

        var otherUser = new User { Id = otherUserId };
        var users = new List<User> { otherUser }.AsQueryable();
        _mockContext.Setup(x => x.Users)
            .Returns(DbContextMockHelper.BuildMockDbSet(users).Object);

        var user = new User { Id = userId };
        _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { AppRoles.Director });
        _mockUserManager.Setup(x => x.GetRolesAsync(otherUser))
            .ReturnsAsync(new List<string> { AppRoles.Employee });

        var command = new UpdateEmployeeCommand
        {
            Id = employeeId,
            DepartmentId = departmentId,
            Position = "Developer",
            EmploymentType = "FullTime",
            Status = "Active",
            Role = AppRoles.Employee
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Be("Không thể gỡ vai trò Director cuối cùng trong doanh nghiệp.");
        _mockUserManager.Verify(x => x.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenDirectorDemotesDirectorAndAnotherDirectorExists_Succeeds()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var otherDirectorEmployeeId = Guid.NewGuid();
        var otherDirectorUserId = Guid.NewGuid();
        var departmentId = 1;
        IEnumerable<string>? removedRoles = null;

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);
        _mockCurrentUserService.SetupGet(x => x.Roles)
            .Returns(new[] { AppRoles.Director });

        var employee = new Employee
        {
            Id = employeeId,
            UserId = userId,
            EnterpriseId = enterpriseId,
            DepartmentId = departmentId,
            IsDeleted = false
        };

        var otherDirectorEmployee = new Employee
        {
            Id = otherDirectorEmployeeId,
            UserId = otherDirectorUserId,
            EnterpriseId = enterpriseId,
            DepartmentId = departmentId,
            IsDeleted = false
        };

        var employees = new List<Employee> { employee, otherDirectorEmployee }.AsQueryable();
        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        var department = new Department
        {
            Id = departmentId,
            EnterpriseId = enterpriseId,
            IsDeleted = false
        };

        var departments = new List<Department> { department }.AsQueryable();
        _mockContext.Setup(x => x.Departments)
            .Returns(DbContextMockHelper.BuildMockDbSet(departments).Object);

        var otherDirectorUser = new User { Id = otherDirectorUserId };
        var users = new List<User> { otherDirectorUser }.AsQueryable();
        _mockContext.Setup(x => x.Users)
            .Returns(DbContextMockHelper.BuildMockDbSet(users).Object);

        var user = new User { Id = userId };
        _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { AppRoles.Director, AppRoles.Employee });
        _mockUserManager.Setup(x => x.GetRolesAsync(otherDirectorUser))
            .ReturnsAsync(new List<string> { AppRoles.Director });
        _mockUserManager.Setup(x => x.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()))
            .Callback<User, IEnumerable<string>>((_, roles) => removedRoles = roles.ToArray())
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(x => x.AddToRoleAsync(user, AppRoles.Employee))
            .ReturnsAsync(IdentityResult.Success);

        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateEmployeeCommand
        {
            Id = employeeId,
            DepartmentId = departmentId,
            Position = "Developer",
            EmploymentType = "FullTime",
            Status = "Active",
            Role = AppRoles.Employee
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        removedRoles.Should().NotBeNull();
        removedRoles.Should().Contain(AppRoles.Director);
        removedRoles.Should().Contain(AppRoles.Employee);
        _mockUserManager.Verify(x => x.AddToRoleAsync(user, AppRoles.Employee), Times.Once);
    }
}
