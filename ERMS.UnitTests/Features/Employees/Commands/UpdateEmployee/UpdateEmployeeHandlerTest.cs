using ERMS.Application.Features.Employees.Commands.UpdateEmployee;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Organization;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ERMS.UnitTests.Features.Employees.Commands.UpdateEmployee;

public class UpdateEmployeeHandlerTest
{
    private readonly Mock<IERMSDbContext> _mockContext;
    private readonly Mock<ILogger<UpdateEmployeeHandler>> _mockLogger;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly UpdateEmployeeHandler _handler;

    public UpdateEmployeeHandlerTest()
    {
        _mockContext = new Mock<IERMSDbContext>();
        _mockLogger = new Mock<ILogger<UpdateEmployeeHandler>>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();

        _handler = new UpdateEmployeeHandler(
            _mockContext.Object,
            _mockLogger.Object,
            _mockCurrentUserService.Object
        );
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

        var employees = new List<Employee> { employee }.AsQueryable();
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
}