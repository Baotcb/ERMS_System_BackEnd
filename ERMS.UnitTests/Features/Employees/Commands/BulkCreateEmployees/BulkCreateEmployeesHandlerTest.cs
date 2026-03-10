using ERMS.Application.Features.Employees.Commands.BulkCreateEmployees;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Enterprise;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Organization;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ERMS.UnitTests.Features.Employees.Commands.BulkCreateEmployees;

public class BulkCreateEmployeesHandlerTest
{
    private readonly Mock<IERMSDbContext> _mockContext;
    private readonly Mock<UserManager<User>> _mockUserManager;
    private readonly Mock<ILogger<BulkCreateEmployeesHandler>> _mockLogger;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly BulkCreateEmployeesHandler _handler;

    public BulkCreateEmployeesHandlerTest()
    {
        _mockContext = new Mock<IERMSDbContext>();
        _mockUserManager = MockUserManager();
        _mockLogger = new Mock<ILogger<BulkCreateEmployeesHandler>>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockEmailService = new Mock<IEmailService>();

        _handler = new BulkCreateEmployeesHandler(
            _mockContext.Object,
            _mockUserManager.Object,
            _mockLogger.Object,
            _mockCurrentUserService.Object,
            _mockEmailService.Object
        );
    }

    [Fact]
    public async Task Handle_WhenUserNotBelongToEnterprise_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync((Guid?)null);

        var command = new BulkCreateEmployeesCommand { Items = new List<EmployeeImportItem>() };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Be("Người dùng không thuộc doanh nghiệp nào.");
    }

    [Fact]
    public async Task Handle_WhenEnterpriseNotFound_ThrowsException()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        var enterprises = new List<Enterprise>().AsQueryable();
        _mockContext.Setup(x => x.Enterprises)
            .Returns(DbContextMockHelper.BuildMockDbSet(enterprises).Object);

        var command = new BulkCreateEmployeesCommand { Items = new List<EmployeeImportItem>() };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Be("Doanh nghiệp không tồn tại");
    }

    [Fact]
    public async Task Handle_WhenDepartmentNotFound_AddsError()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var enterprise = new Enterprise
        {
            Id = enterpriseId,
            EnterpriseCode = "ENT",
            EnterpriseName = "Test Enterprise",
            IsDeleted = false
        };

        SetupMockContext(enterpriseId, enterprise, new List<Department>(), 0);

        var command = new BulkCreateEmployeesCommand
        {
            Items = new List<EmployeeImportItem>
            {
                new EmployeeImportItem
                {
                    Email = "test@example.com",
                    FullName = "Test User",
                    DepartmentCode = "INVALID",
                    Position = "Developer",
                    Phone = "0123456789",
                    Password = "Password123!"
                }
            }
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(1);
        result.SuccessCount.Should().Be(0);
        result.FailedCount.Should().Be(1);
        result.Errors.Should().HaveCount(1);
        result.Errors[0].RowIndex.Should().Be(1);
        result.Errors[0].Email.Should().Be("test@example.com");
        result.Errors[0].ErrorMessage.Should().Contain("không tồn tại");
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_AddsError()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var enterprise = new Enterprise
        {
            Id = enterpriseId,
            EnterpriseCode = "ENT",
            EnterpriseName = "Test Enterprise",
            IsDeleted = false
        };

        var department = new Department
        {
            Id = 1,
            DepartmentCode = "IT",
            EnterpriseId = enterpriseId,
            IsDeleted = false
        };

        SetupMockContext(enterpriseId, enterprise, new List<Department> { department }, 0);

        var existingUser = new User { Email = "test@example.com" };
        _mockUserManager.Setup(x => x.FindByEmailAsync("test@example.com"))
            .ReturnsAsync(existingUser);

        var command = new BulkCreateEmployeesCommand
        {
            Items = new List<EmployeeImportItem>
            {
                new EmployeeImportItem
                {
                    Email = "test@example.com",
                    FullName = "Test User",
                    DepartmentCode = "IT",
                    Position = "Developer",
                    Phone = "0123456789",
                    Password = "Password123!"
                }
            }
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(1);
        result.SuccessCount.Should().Be(0);
        result.FailedCount.Should().Be(1);
        result.Errors.Should().HaveCount(1);
        result.Errors[0].ErrorMessage.Should().Be("Email đã được sử dụng");
    }

    [Fact]
    public async Task Handle_WhenUserCreationFails_AddsError()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var enterprise = new Enterprise
        {
            Id = enterpriseId,
            EnterpriseCode = "ENT",
            EnterpriseName = "Test Enterprise",
            IsDeleted = false
        };

        var department = new Department
        {
            Id = 1,
            DepartmentCode = "IT",
            EnterpriseId = enterpriseId,
            IsDeleted = false
        };

        SetupMockContext(enterpriseId, enterprise, new List<Department> { department }, 0);

        _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        var identityErrors = new[] { new IdentityError { Description = "Password too weak" } };
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(identityErrors));

        var command = new BulkCreateEmployeesCommand
        {
            Items = new List<EmployeeImportItem>
            {
                new EmployeeImportItem
                {
                    Email = "newuser@example.com",
                    FullName = "New User",
                    DepartmentCode = "IT",
                    Position = "Developer",
                    Phone = "0123456789",
                    Password = "weak"
                }
            }
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(1);
        result.SuccessCount.Should().Be(0);
        result.FailedCount.Should().Be(1);
        result.Errors.Should().HaveCount(1);
        result.Errors[0].ErrorMessage.Should().Contain("Password too weak");
    }

    [Fact]
    public async Task Handle_WhenValidData_CreatesEmployeeSuccessfully()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var enterprise = new Enterprise
        {
            Id = enterpriseId,
            EnterpriseCode = "ENT",
            EnterpriseName = "Test Enterprise",
            IsDeleted = false
        };

        var department = new Department
        {
            Id = 1,
            DepartmentCode = "IT",
            EnterpriseId = enterpriseId,
            IsDeleted = false
        };

        SetupMockContext(enterpriseId, enterprise, new List<Department> { department }, 0);

        _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var command = new BulkCreateEmployeesCommand
        {
            Items = new List<EmployeeImportItem>
            {
                new EmployeeImportItem
                {
                    Email = "newuser@example.com",
                    FullName = "New User",
                    DepartmentCode = "IT",
                    Position = "Developer",
                    Phone = "0123456789",
                    Password = "Password123!",
                    Role = "Employee"
                }
            }
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(1);
        result.SuccessCount.Should().Be(1);
        result.FailedCount.Should().Be(0);
        result.Errors.Should().BeEmpty();

        _mockContext.Verify(x => x.Employees.Add(It.IsAny<Employee>()), Times.Once);
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUserManager.Verify(x => x.AddToRoleAsync(It.IsAny<User>(), "Employee"), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPasswordProvided_UsesProvidedPassword()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var enterprise = new Enterprise
        {
            Id = enterpriseId,
            EnterpriseCode = "ENT",
            EnterpriseName = "Test Enterprise",
            IsDeleted = false
        };

        var department = new Department
        {
            Id = 1,
            DepartmentCode = "IT",
            EnterpriseId = enterpriseId,
            IsDeleted = false
        };

        SetupMockContext(enterpriseId, enterprise, new List<Department> { department }, 0);

        _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        string? capturedPassword = null;
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .Callback<User, string>((user, password) => capturedPassword = password)
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var expectedPassword = "MyPassword123!";
        var command = new BulkCreateEmployeesCommand
        {
            Items = new List<EmployeeImportItem>
            {
                new EmployeeImportItem
                {
                    Email = "newuser@example.com",
                    FullName = "New User",
                    DepartmentCode = "IT",
                    Position = "Developer",
                    Phone = "0123456789",
                    Password = expectedPassword
                }
            }
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedPassword.Should().Be(expectedPassword);
    }

    [Fact]
    public async Task Handle_WhenPasswordEmpty_GeneratesRandomPassword()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var enterprise = new Enterprise
        {
            Id = enterpriseId,
            EnterpriseCode = "ENT",
            EnterpriseName = "Test Enterprise",
            IsDeleted = false
        };

        var department = new Department
        {
            Id = 1,
            DepartmentCode = "IT",
            EnterpriseId = enterpriseId,
            IsDeleted = false
        };

        SetupMockContext(enterpriseId, enterprise, new List<Department> { department }, 0);

        _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        string? capturedPassword = null;
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .Callback<User, string>((user, password) => capturedPassword = password)
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var command = new BulkCreateEmployeesCommand
        {
            Items = new List<EmployeeImportItem>
            {
                new EmployeeImportItem
                {
                    Email = "newuser@example.com",
                    FullName = "New User",
                    DepartmentCode = "IT",
                    Position = "Developer",
                    Phone = "0123456789",
                    Password = "" // Empty password
                }
            }
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedPassword.Should().NotBeNullOrEmpty();
        capturedPassword!.Length.Should().BeGreaterThanOrEqualTo(12);
    }

    [Fact]
    public async Task Handle_WhenMultipleItems_ProcessesAll()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var enterprise = new Enterprise
        {
            Id = enterpriseId,
            EnterpriseCode = "ENT",
            EnterpriseName = "Test Enterprise",
            IsDeleted = false
        };

        var department = new Department
        {
            Id = 1,
            DepartmentCode = "IT",
            EnterpriseId = enterpriseId,
            IsDeleted = false
        };

        SetupMockContext(enterpriseId, enterprise, new List<Department> { department }, 0);

        _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var command = new BulkCreateEmployeesCommand
        {
            Items = new List<EmployeeImportItem>
            {
                new EmployeeImportItem
                {
                    Email = "user1@example.com",
                    FullName = "User 1",
                    DepartmentCode = "IT",
                    Position = "Developer",
                    Password = "Password123!"
                },
                new EmployeeImportItem
                {
                    Email = "user2@example.com",
                    FullName = "User 2",
                    DepartmentCode = "IT",
                    Position = "Tester",
                    Password = "Password123!"
                }
            }
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(2);
        result.SuccessCount.Should().Be(2);
        result.FailedCount.Should().Be(0);
        _mockContext.Verify(x => x.Employees.Add(It.IsAny<Employee>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_WhenMixedSuccessAndFailure_ReturnsCorrectCounts()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var enterprise = new Enterprise
        {
            Id = enterpriseId,
            EnterpriseCode = "ENT",
            EnterpriseName = "Test Enterprise",
            IsDeleted = false
        };

        var department = new Department
        {
            Id = 1,
            DepartmentCode = "IT",
            EnterpriseId = enterpriseId,
            IsDeleted = false
        };

        SetupMockContext(enterpriseId, enterprise, new List<Department> { department }, 0);

        _mockUserManager.Setup(x => x.FindByEmailAsync("success@example.com"))
            .ReturnsAsync((User?)null);

        _mockUserManager.Setup(x => x.FindByEmailAsync("existing@example.com"))
            .ReturnsAsync(new User { Email = "existing@example.com" });

        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var command = new BulkCreateEmployeesCommand
        {
            Items = new List<EmployeeImportItem>
            {
                new EmployeeImportItem
                {
                    Email = "success@example.com",
                    FullName = "Success User",
                    DepartmentCode = "IT",
                    Position = "Developer",
                    Password = "Password123!"
                },
                new EmployeeImportItem
                {
                    Email = "existing@example.com",
                    FullName = "Existing User",
                    DepartmentCode = "IT",
                    Position = "Tester",
                    Password = "Password123!"
                }
            }
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(2);
        result.SuccessCount.Should().Be(1);
        result.FailedCount.Should().Be(1);
        result.Errors.Should().HaveCount(1);
        result.Errors[0].RowIndex.Should().Be(2);
        result.Errors[0].Email.Should().Be("existing@example.com");
    }

    [Fact]
    public async Task Handle_WhenRoleProvided_UsesProvidedRole()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var enterprise = new Enterprise
        {
            Id = enterpriseId,
            EnterpriseCode = "ENT",
            EnterpriseName = "Test Enterprise",
            IsDeleted = false
        };

        var department = new Department
        {
            Id = 1,
            DepartmentCode = "IT",
            EnterpriseId = enterpriseId,
            IsDeleted = false
        };

        SetupMockContext(enterpriseId, enterprise, new List<Department> { department }, 0);

        _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var command = new BulkCreateEmployeesCommand
        {
            Items = new List<EmployeeImportItem>
            {
                new EmployeeImportItem
                {
                    Email = "trainer@example.com",
                    FullName = "Trainer User",
                    DepartmentCode = "IT",
                    Position = "Trainer",
                    Password = "Password123!",
                    Role = "Trainer"
                }
            }
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockUserManager.Verify(x => x.AddToRoleAsync(It.IsAny<User>(), "Trainer"), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRoleInvalidOrEmpty_DefaultsToEmployee()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var enterprise = new Enterprise
        {
            Id = enterpriseId,
            EnterpriseCode = "ENT",
            EnterpriseName = "Test Enterprise",
            IsDeleted = false
        };

        var department = new Department
        {
            Id = 1,
            DepartmentCode = "IT",
            EnterpriseId = enterpriseId,
            IsDeleted = false
        };

        SetupMockContext(enterpriseId, enterprise, new List<Department> { department }, 0);

        _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var command = new BulkCreateEmployeesCommand
        {
            Items = new List<EmployeeImportItem>
            {
                new EmployeeImportItem
                {
                    Email = "user@example.com",
                    FullName = "User",
                    DepartmentCode = "IT",
                    Position = "Developer",
                    Password = "Password123!",
                    Role = "InvalidRole"
                }
            }
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockUserManager.Verify(x => x.AddToRoleAsync(It.IsAny<User>(), "Employee"), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCreatingEmployee_GeneratesCorrectEmployeeCode()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var enterprise = new Enterprise
        {
            Id = enterpriseId,
            EnterpriseCode = "COMP",
            EnterpriseName = "Test Enterprise",
            IsDeleted = false
        };

        var department = new Department
        {
            Id = 1,
            DepartmentCode = "IT",
            EnterpriseId = enterpriseId,
            IsDeleted = false
        };

        SetupMockContext(enterpriseId, enterprise, new List<Department> { department }, 5); // 5 existing employees

        _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        Employee? capturedEmployee = null;
        _mockContext.Setup(x => x.Employees.Add(It.IsAny<Employee>()))
            .Callback<Employee>(emp => capturedEmployee = emp);

        var command = new BulkCreateEmployeesCommand
        {
            Items = new List<EmployeeImportItem>
            {
                new EmployeeImportItem
                {
                    Email = "employee@example.com",
                    FullName = "Employee Name",
                    DepartmentCode = "IT",
                    Position = "Developer",
                    Password = "Password123!"
                }
            }
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedEmployee.Should().NotBeNull();
        capturedEmployee!.EmployeeCode.Should().Be("COMP-0006");
    }

    [Fact]
    public async Task Handle_WhenException_AddsErrorForThatRow()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var enterprise = new Enterprise
        {
            Id = enterpriseId,
            EnterpriseCode = "ENT",
            EnterpriseName = "Test Enterprise",
            IsDeleted = false
        };

        var department = new Department
        {
            Id = 1,
            DepartmentCode = "IT",
            EnterpriseId = enterpriseId,
            IsDeleted = false
        };

        SetupMockContext(enterpriseId, enterprise, new List<Department> { department }, 0);

        _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        var command = new BulkCreateEmployeesCommand
        {
            Items = new List<EmployeeImportItem>
            {
                new EmployeeImportItem
                {
                    Email = "test@example.com",
                    FullName = "Test User",
                    DepartmentCode = "IT",
                    Position = "Developer",
                    Password = "Password123!"
                }
            }
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(1);
        result.SuccessCount.Should().Be(0);
        result.FailedCount.Should().Be(1);
        result.Errors.Should().HaveCount(1);
        result.Errors[0].ErrorMessage.Should().Contain("Database connection failed");
    }

    [Fact]
    public async Task Handle_WhenSuccessful_LogsInformation()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var enterprise = new Enterprise
        {
            Id = enterpriseId,
            EnterpriseCode = "ENT",
            EnterpriseName = "Test Enterprise",
            IsDeleted = false
        };

        var department = new Department
        {
            Id = 1,
            DepartmentCode = "IT",
            EnterpriseId = enterpriseId,
            IsDeleted = false
        };

        SetupMockContext(enterpriseId, enterprise, new List<Department> { department }, 0);

        _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var command = new BulkCreateEmployeesCommand
        {
            Items = new List<EmployeeImportItem>
            {
                new EmployeeImportItem
                {
                    Email = "user@example.com",
                    FullName = "User",
                    DepartmentCode = "IT",
                    Position = "Developer",
                    Password = "Password123!"
                }
            }
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Bulk created")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCreatingEmployee_SetsCorrectProperties()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var enterprise = new Enterprise
        {
            Id = enterpriseId,
            EnterpriseCode = "ENT",
            EnterpriseName = "Test Enterprise",
            IsDeleted = false
        };

        var department = new Department
        {
            Id = 1,
            DepartmentCode = "IT",
            EnterpriseId = enterpriseId,
            IsDeleted = false
        };

        SetupMockContext(enterpriseId, enterprise, new List<Department> { department }, 0);

        _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        Employee? capturedEmployee = null;
        _mockContext.Setup(x => x.Employees.Add(It.IsAny<Employee>()))
            .Callback<Employee>(emp => capturedEmployee = emp);

        var command = new BulkCreateEmployeesCommand
        {
            Items = new List<EmployeeImportItem>
            {
                new EmployeeImportItem
                {
                    Email = "employee@example.com",
                    FullName = "Employee Name",
                    DepartmentCode = "IT",
                    Position = "Senior Developer",
                    Password = "Password123!"
                }
            }
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedEmployee.Should().NotBeNull();
        capturedEmployee!.EnterpriseId.Should().Be(enterpriseId);
        capturedEmployee.DepartmentId.Should().Be(1);
        capturedEmployee.Position.Should().Be("Senior Developer");
        capturedEmployee.EmploymentType.Should().Be("FullTime");
        capturedEmployee.Status.Should().Be("Active");
        capturedEmployee.IsDeleted.Should().BeFalse();
        capturedEmployee.HireDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        capturedEmployee.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // Helper Methods
    private void SetupMockContext(Guid enterpriseId, Enterprise enterprise, List<Department> departments, int employeeCount)
    {
        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        var enterprises = new List<Enterprise> { enterprise }.AsQueryable();
        _mockContext.Setup(x => x.Enterprises)
            .Returns(DbContextMockHelper.BuildMockDbSet(enterprises).Object);

        var departmentQuery = departments.AsQueryable();
        _mockContext.Setup(x => x.Departments)
            .Returns(DbContextMockHelper.BuildMockDbSet(departmentQuery).Object);

            // Create a list with the specified number of employees
        var employees = Enumerable.Range(0, employeeCount)
            .Select(i => new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeCode = $"EMP{i + 1:D4}",
                EnterpriseId = enterpriseId
            })
            .ToList()
            .AsQueryable();

        var employeesDbSet = DbContextMockHelper.BuildMockDbSet(employees);
        _mockContext.Setup(x => x.Employees).Returns(employeesDbSet.Object);

        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    private static Mock<UserManager<User>> MockUserManager()
    {
        var store = new Mock<IUserStore<User>>();
        return new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
    }
}