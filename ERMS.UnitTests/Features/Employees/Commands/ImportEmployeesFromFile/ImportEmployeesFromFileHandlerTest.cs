using ERMS.Application.Interface;
using ERMS.Application.Features.Employees.Commands.ImportEmployeesFromFile;
using ERMS.Domain.Entities.Enterprise;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Organization;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using Xunit;

namespace ERMS.UnitTests.Features.Employees.Commands.ImportEmployeesFromFile;

public class ImportEmployeesFromFileHandlerTest
{
    private readonly Mock<IERMSDbContext> _mockContext;
    private readonly Mock<UserManager<User>> _mockUserManager;
    private readonly Mock<ILogger<ImportEmployeesFromFileHandler>> _mockLogger;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IExcelParserService> _mockExcelParser;
    private readonly ImportEmployeesFromFileHandler _handler;

    public ImportEmployeesFromFileHandlerTest()
    {
        _mockContext = new Mock<IERMSDbContext>();
        _mockUserManager = MockUserManager();
        _mockLogger = new Mock<ILogger<ImportEmployeesFromFileHandler>>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockExcelParser = new Mock<IExcelParserService>();

        _handler = new ImportEmployeesFromFileHandler(
            _mockContext.Object,
            _mockUserManager.Object,
            _mockLogger.Object,
            _mockCurrentUserService.Object,
            _mockEmailService.Object,
            _mockExcelParser.Object
        );
    }

    [Fact]
    public async Task Handle_WhenParseResultInvalid_ReturnsErrorsFromParser()
    {
        // Arrange
        var fileMock = CreateMockFormFile("test.xlsx", "fake excel content");
        var command = new ImportEmployeesFromFileCommand
        {
            File = fileMock,
            Commit = false
        };

        var parseResult = new ExcelParseResult
        {
            Errors = new List<ParseError>
            {
                new ParseError
                {
                    RowNumber = 1,
                    Column = "Email",
                    Message = "Invalid email format"
                }
            },
            MissingRequiredColumns = new List<string> { "FullName" }
        };

        _mockExcelParser.Setup(x => x.ParseEmployeeImportFile(It.IsAny<Stream>(), It.IsAny<string>()))
            .Returns(parseResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Errors.Should().HaveCount(1);
        result.Errors[0].RowNumber.Should().Be(1);
        result.Errors[0].Column.Should().Be("Email");
        result.Errors[0].Message.Should().Be("Invalid email format");
    }

    [Fact]
    public async Task Handle_WhenUserNotBelongToEnterprise_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync((Guid?)null);

        var fileMock = CreateMockFormFile("test.xlsx", "fake excel content");
        var command = new ImportEmployeesFromFileCommand
        {
            File = fileMock,
            Commit = false
        };

        var parseResult = new ExcelParseResult
        {
            Rows = new List<ParsedEmployeeRow>()
        };
        _mockExcelParser.Setup(x => x.ParseEmployeeImportFile(It.IsAny<Stream>(), It.IsAny<string>()))
            .Returns(parseResult);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(command, CancellationToken.None));
        exception.Message.Should().Contain("thông tin doanh nghiệp");
    }

    [Fact]
    public async Task Handle_WhenUserNotHRManager_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);
        _mockCurrentUserService.Setup(x => x.Roles)
            .Returns(new List<string> { "Employee" });

        var fileMock = CreateMockFormFile("test.xlsx", "fake excel content");
        var command = new ImportEmployeesFromFileCommand
        {
            File = fileMock,
            Commit = false
        };

        var parseResult = new ExcelParseResult
        {
            Rows = new List<ParsedEmployeeRow>()
        };
        _mockExcelParser.Setup(x => x.ParseEmployeeImportFile(It.IsAny<Stream>(), It.IsAny<string>()))
            .Returns(parseResult);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(command, CancellationToken.None));
        exception.Message.Should().Contain("HR Manager");
    }

    [Fact]
    public async Task Handle_WhenEnterpriseNotFound_ThrowsException()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);
        _mockCurrentUserService.Setup(x => x.Roles)
            .Returns(new List<string> { "HRManager" });

        var enterprises = new List<Enterprise>().AsQueryable();
        _mockContext.Setup(x => x.Enterprises).Returns(DbContextMockHelper.BuildMockDbSet(enterprises).Object);

        var fileMock = CreateMockFormFile("test.xlsx", "fake excel content");
        var command = new ImportEmployeesFromFileCommand
        {
            File = fileMock,
            Commit = false
        };

        var parseResult = new ExcelParseResult
        {
            Rows = new List<ParsedEmployeeRow>()
        };
        _mockExcelParser.Setup(x => x.ParseEmployeeImportFile(It.IsAny<Stream>(), It.IsAny<string>()))
            .Returns(parseResult);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _handler.Handle(command, CancellationToken.None));
        exception.Message.Should().Be("Doanh nghiệp không tồn tại");
    }

    [Fact]
    public async Task Handle_WhenDryRunWithDuplicateEmailInFile_ReturnsError()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var department = new Department
        {
            Id = 1,
            DepartmentCode = "IT",
            EnterpriseId = enterpriseId,
            IsDeleted = false
        };

        SetupBasicContext(enterpriseId, new List<Department> { department });

        var fileMock = CreateMockFormFile("test.xlsx", "fake excel content");
        var command = new ImportEmployeesFromFileCommand
        {
            File = fileMock,
            Commit = false
        };

        var parseResult = new ExcelParseResult
        {
            Rows = new List<ParsedEmployeeRow>
            {
                new ParsedEmployeeRow
                {
                    RowNumber = 2,
                    IsValid = true,
                    Email = "duplicate@example.com",
                    FullName = "User 1",
                    DepartmentCode = "IT",
                    Position = "Developer"
                },
                new ParsedEmployeeRow
                {
                    RowNumber = 3,
                    IsValid = true,
                    Email = "duplicate@example.com",
                    FullName = "User 2",
                    DepartmentCode = "IT",
                    Position = "Tester"
                }
            }
        };

        _mockExcelParser.Setup(x => x.ParseEmployeeImportFile(It.IsAny<Stream>(), It.IsAny<string>()))
            .Returns(parseResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Errors.Should().Contain(e => e.Message.Contains("trùng lặp trong file Excel"));
        result.FailedCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Handle_WhenDryRunWithInvalidDepartment_ReturnsError()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        SetupBasicContext(enterpriseId, new List<Department>());

        var fileMock = CreateMockFormFile("test.xlsx", "fake excel content");
        var command = new ImportEmployeesFromFileCommand
        {
            File = fileMock,
            Commit = false
        };

        var parseResult = new ExcelParseResult
        {
            Rows = new List<ParsedEmployeeRow>
            {
                new ParsedEmployeeRow
                {
                    RowNumber = 2,
                    IsValid = true,
                    Email = "test@example.com",
                    FullName = "Test User",
                    DepartmentCode = "INVALID",
                    Position = "Developer"
                }
            }
        };

        _mockExcelParser.Setup(x => x.ParseEmployeeImportFile(It.IsAny<Stream>(), It.IsAny<string>()))
            .Returns(parseResult);

        _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Errors.Should().Contain(e => e.Column == "DepartmentCode" && e.Message.Contains("không tồn tại"));
        result.FailedCount.Should().Be(1);
        result.SuccessCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenDryRunWithDirectorWithoutDepartment_AllowsRow()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        SetupBasicContext(enterpriseId, new List<Department>());

        var fileMock = CreateMockFormFile("test.xlsx", "fake excel content");
        var command = new ImportEmployeesFromFileCommand
        {
            File = fileMock,
            Commit = false
        };

        var parseResult = new ExcelParseResult
        {
            Rows = new List<ParsedEmployeeRow>
            {
                new ParsedEmployeeRow
                {
                    RowNumber = 2,
                    IsValid = true,
                    Email = "director@example.com",
                    FullName = "Director User",
                    DepartmentCode = null,
                    Role = "Director"
                }
            }
        };

        _mockExcelParser.Setup(x => x.ParseEmployeeImportFile(It.IsAny<Stream>(), It.IsAny<string>()))
            .Returns(parseResult);

        _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Errors.Should().NotContain(e => e.Column == "DepartmentCode");
        result.SuccessCount.Should().Be(1);
        result.FailedCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenDryRunWithExistingEmail_ReturnsError()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var department = new Department
        {
            Id = 1,
            DepartmentCode = "IT",
            EnterpriseId = enterpriseId,
            IsDeleted = false
        };

        SetupBasicContext(enterpriseId, new List<Department> { department });

        var fileMock = CreateMockFormFile("test.xlsx", "fake excel content");
        var command = new ImportEmployeesFromFileCommand
        {
            File = fileMock,
            Commit = false
        };

        var parseResult = new ExcelParseResult
        {
            Rows = new List<ParsedEmployeeRow>
            {
                new ParsedEmployeeRow
                {
                    RowNumber = 2,
                    IsValid = true,
                    Email = "existing@example.com",
                    FullName = "Test User",
                    DepartmentCode = "IT",
                    Position = "Developer"
                }
            }
        };

        _mockExcelParser.Setup(x => x.ParseEmployeeImportFile(It.IsAny<Stream>(), It.IsAny<string>()))
            .Returns(parseResult);

        var existingUser = new User { Email = "existing@example.com" };
        _mockUserManager.Setup(x => x.FindByEmailAsync("existing@example.com"))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Errors.Should().Contain(e => e.Column == "Email" && e.Message.Contains("đã tồn tại"));
        result.FailedCount.Should().Be(1);
        result.SuccessCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenDryRunWithValidData_ReturnsSuccessCount()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var department = new Department
        {
            Id = 1,
            DepartmentCode = "IT",
            EnterpriseId = enterpriseId,
            IsDeleted = false
        };

        SetupBasicContext(enterpriseId, new List<Department> { department });

        var fileMock = CreateMockFormFile("test.xlsx", "fake excel content");
        var command = new ImportEmployeesFromFileCommand
        {
            File = fileMock,
            Commit = false
        };

        var parseResult = new ExcelParseResult
        {
            Rows = new List<ParsedEmployeeRow>
            {
                new ParsedEmployeeRow
                {
                    RowNumber = 2,
                    IsValid = true,
                    Email = "newuser@example.com",
                    FullName = "New User",
                    DepartmentCode = "IT",
                    Position = "Developer",
                    Phone = "0123456789"
                }
            }
        };

        _mockExcelParser.Setup(x => x.ParseEmployeeImportFile(It.IsAny<Stream>(), It.IsAny<string>()))
            .Returns(parseResult);

        _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.TotalRows.Should().Be(1);
        result.ValidRows.Should().Be(1);
        result.SuccessCount.Should().Be(1);
        result.FailedCount.Should().Be(0);
        result.Errors.Should().BeEmpty();

        // Verify no actual creation happened (dry run)
        _mockUserManager.Verify(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCommitWithValidData_CreatesEmployees()
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

        SetupBasicContext(enterpriseId, new List<Department> { department }, enterprise);

        var fileMock = CreateMockFormFile("test.xlsx", "fake excel content");
        var command = new ImportEmployeesFromFileCommand
        {
            File = fileMock,
            Commit = true
        };

        var parseResult = new ExcelParseResult
        {
            Rows = new List<ParsedEmployeeRow>
            {
                new ParsedEmployeeRow
                {
                    RowNumber = 2,
                    IsValid = true,
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

        _mockExcelParser.Setup(x => x.ParseEmployeeImportFile(It.IsAny<Stream>(), It.IsAny<string>()))
            .Returns(parseResult);

        _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.TotalRows.Should().Be(1);
        result.ValidRows.Should().Be(1);
        result.SuccessCount.Should().Be(1);
        result.FailedCount.Should().Be(0);
        result.Errors.Should().BeEmpty();

        _mockUserManager.Verify(x => x.CreateAsync(It.IsAny<User>(), "Password123!"), Times.Once);
        _mockUserManager.Verify(x => x.AddToRoleAsync(It.IsAny<User>(), "Employee"), Times.Once);
        _mockContext.Verify(x => x.Employees.Add(It.IsAny<Employee>()), Times.Once);
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCommitWithSkillDescription_MapsSkillDescriptionToEmployee()
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

        SetupBasicContext(enterpriseId, new List<Department> { department }, enterprise);

        var fileMock = CreateMockFormFile("test.xlsx", "fake excel content");
        var command = new ImportEmployeesFromFileCommand
        {
            File = fileMock,
            Commit = true
        };

        var parseResult = new ExcelParseResult
        {
            Rows = new List<ParsedEmployeeRow>
            {
                new ParsedEmployeeRow
                {
                    RowNumber = 2,
                    IsValid = true,
                    Email = "newuser@example.com",
                    FullName = "New User",
                    DepartmentCode = "IT",
                    Position = "Developer",
                    Phone = "0123456789",
                    Password = "Password123!",
                    Role = "Employee",
                    SkillDescription = "Kỹ năng phù hợp: C#, .NET"
                }
            }
        };

        _mockExcelParser.Setup(x => x.ParseEmployeeImportFile(It.IsAny<Stream>(), It.IsAny<string>()))
            .Returns(parseResult);

        _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        Employee? createdEmployee = null;
        var employeeSet = new List<Employee>().AsQueryable().BuildMockDbSet();
        employeeSet.Setup(x => x.Add(It.IsAny<Employee>()))
            .Callback<Employee>(employee => createdEmployee = employee);
        _mockContext.Setup(x => x.Employees).Returns(employeeSet.Object);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.SuccessCount.Should().Be(1);
        createdEmployee.Should().NotBeNull();
        createdEmployee!.SkillDescription.Should().Be("Kỹ năng phù hợp: C#, .NET");
    }

    [Fact]
    public async Task Handle_WhenCommitWithDirectorWithoutDepartment_CreatesEmployeeWithNullDepartment()
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

        SetupBasicContext(enterpriseId, new List<Department>(), enterprise);

        var fileMock = CreateMockFormFile("test.xlsx", "fake excel content");
        var command = new ImportEmployeesFromFileCommand
        {
            File = fileMock,
            Commit = true
        };

        var parseResult = new ExcelParseResult
        {
            Rows = new List<ParsedEmployeeRow>
            {
                new ParsedEmployeeRow
                {
                    RowNumber = 2,
                    IsValid = true,
                    Email = "director@example.com",
                    FullName = "Director User",
                    DepartmentCode = null,
                    Position = "Director",
                    Password = "Password123!",
                    Role = "Director"
                }
            }
        };

        _mockExcelParser.Setup(x => x.ParseEmployeeImportFile(It.IsAny<Stream>(), It.IsAny<string>()))
            .Returns(parseResult);

        _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.SuccessCount.Should().Be(1);
        result.FailedCount.Should().Be(0);
        _mockUserManager.Verify(x => x.AddToRoleAsync(It.IsAny<User>(), "Director"), Times.Once);
        _mockContext.Verify(x => x.Employees.Add(It.Is<Employee>(e => e.DepartmentId == null)), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCommitWithNoPassword_GeneratesRandomPassword()
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

        SetupBasicContext(enterpriseId, new List<Department> { department }, enterprise);

        var fileMock = CreateMockFormFile("test.xlsx", "fake excel content");
        var command = new ImportEmployeesFromFileCommand
        {
            File = fileMock,
            Commit = true
        };

        var parseResult = new ExcelParseResult
        {
            Rows = new List<ParsedEmployeeRow>
            {
                new ParsedEmployeeRow
                {
                    RowNumber = 2,
                    IsValid = true,
                    Email = "newuser@example.com",
                    FullName = "New User",
                    DepartmentCode = "IT",
                    Position = "Developer",
                    Password = null // No password provided
                }
            }
        };

        _mockExcelParser.Setup(x => x.ParseEmployeeImportFile(It.IsAny<Stream>(), It.IsAny<string>()))
            .Returns(parseResult);

        _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        string? capturedPassword = null;
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .Callback<User, string>((user, password) => capturedPassword = password)
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.SuccessCount.Should().Be(1);
        capturedPassword.Should().NotBeNullOrEmpty();
        capturedPassword!.Length.Should().BeGreaterThanOrEqualTo(12);
    }

    [Fact]
    public async Task Handle_WhenUserCreationFails_RollsBackAndReturnsError()
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

        SetupBasicContext(enterpriseId, new List<Department> { department }, enterprise);

        var fileMock = CreateMockFormFile("test.xlsx", "fake excel content");
        var command = new ImportEmployeesFromFileCommand
        {
            File = fileMock,
            Commit = true
        };

        var parseResult = new ExcelParseResult
        {
            Rows = new List<ParsedEmployeeRow>
            {
                new ParsedEmployeeRow
                {
                    RowNumber = 2,
                    IsValid = true,
                    Email = "test@example.com",
                    FullName = "Test User",
                    DepartmentCode = "IT",
                    Position = "Developer"
                }
            }
        };

        _mockExcelParser.Setup(x => x.ParseEmployeeImportFile(It.IsAny<Stream>(), It.IsAny<string>()))
            .Returns(parseResult);

        _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        var identityErrors = new[] { new IdentityError { Description = "Password too weak" } };
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(identityErrors));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.SuccessCount.Should().Be(0);
        result.FailedCount.Should().Be(1);
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Column.Should().Be("User");
        result.Errors[0].Message.Should().Contain("Password too weak");
    }

    [Fact]
    public async Task Handle_WhenCommitWithMultipleRows_ProcessesAllRows()
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

        SetupBasicContext(enterpriseId, new List<Department> { department }, enterprise);

        var fileMock = CreateMockFormFile("test.xlsx", "fake excel content");
        var command = new ImportEmployeesFromFileCommand
        {
            File = fileMock,
            Commit = true
        };

        var parseResult = new ExcelParseResult
        {
            Rows = new List<ParsedEmployeeRow>
            {
                new ParsedEmployeeRow
                {
                    RowNumber = 2,
                    IsValid = true,
                    Email = "user1@example.com",
                    FullName = "User 1",
                    DepartmentCode = "IT",
                    Position = "Developer"
                },
                new ParsedEmployeeRow
                {
                    RowNumber = 3,
                    IsValid = true,
                    Email = "user2@example.com",
                    FullName = "User 2",
                    DepartmentCode = "IT",
                    Position = "Tester"
                }
            }
        };

        _mockExcelParser.Setup(x => x.ParseEmployeeImportFile(It.IsAny<Stream>(), It.IsAny<string>()))
            .Returns(parseResult);

        _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.TotalRows.Should().Be(2);
        result.SuccessCount.Should().Be(2);
        result.FailedCount.Should().Be(0);
        _mockUserManager.Verify(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Exactly(2));
        _mockContext.Verify(x => x.Employees.Add(It.IsAny<Employee>()), Times.Exactly(2));
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_WhenCommitWithMixedResults_ReturnsCorrectCounts()
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

        SetupBasicContext(enterpriseId, new List<Department> { department }, enterprise);

        var fileMock = CreateMockFormFile("test.xlsx", "fake excel content");
        var command = new ImportEmployeesFromFileCommand
        {
            File = fileMock,
            Commit = true
        };

        var parseResult = new ExcelParseResult
        {
            Rows = new List<ParsedEmployeeRow>
            {
                new ParsedEmployeeRow
                {
                    RowNumber = 2,
                    IsValid = true,
                    Email = "success@example.com",
                    FullName = "Success User",
                    DepartmentCode = "IT",
                    Position = "Developer"
                },
                new ParsedEmployeeRow
                {
                    RowNumber = 3,
                    IsValid = true,
                    Email = "existing@example.com",
                    FullName = "Existing User",
                    DepartmentCode = "IT",
                    Position = "Tester"
                }
            }
        };

        _mockExcelParser.Setup(x => x.ParseEmployeeImportFile(It.IsAny<Stream>(), It.IsAny<string>()))
            .Returns(parseResult);

        _mockUserManager.Setup(x => x.FindByEmailAsync("success@example.com"))
            .ReturnsAsync((User?)null);

        _mockUserManager.Setup(x => x.FindByEmailAsync("existing@example.com"))
            .ReturnsAsync(new User { Email = "existing@example.com" });

        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.TotalRows.Should().Be(2);
        result.SuccessCount.Should().Be(1);
        result.FailedCount.Should().Be(1);
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Email.Should().Be("existing@example.com");
    }

    [Fact]
    public async Task Handle_WhenParseResultHasWarnings_CopiesWarningsToResult()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var department = new Department
        {
            Id = 1,
            DepartmentCode = "IT",
            EnterpriseId = enterpriseId,
            IsDeleted = false
        };

        SetupBasicContext(enterpriseId, new List<Department> { department });

        var fileMock = CreateMockFormFile("test.xlsx", "fake excel content");
        var command = new ImportEmployeesFromFileCommand
        {
            File = fileMock,
            Commit = false
        };

        var parseResult = new ExcelParseResult
        {
            Rows = new List<ParsedEmployeeRow>(),
            Warnings = new List<ParseWarning>
            {
                new ParseWarning
                {
                    Type = "unknown_column",
                    Column = "MiddleName",
                    Message = "Column not recognized"
                }
            },
            UnknownColumns = new List<string> { "MiddleName" }
        };

        _mockExcelParser.Setup(x => x.ParseEmployeeImportFile(It.IsAny<Stream>(), It.IsAny<string>()))
            .Returns(parseResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Warnings.Should().HaveCount(1);
        result.Warnings[0].Type.Should().Be("unknown_column");
        result.Warnings[0].Column.Should().Be("MiddleName");
        result.UnknownColumns.Should().Contain("MiddleName");
    }

    // Helper Methods
    private void SetupBasicContext(Guid enterpriseId, List<Department> departments, Enterprise? enterprise = null)
    {
        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);
        _mockCurrentUserService.Setup(x => x.Roles)
            .Returns(new List<string> { "HRManager" });

        enterprise ??= new Enterprise
        {
            Id = enterpriseId,
            EnterpriseCode = "ENT",
            EnterpriseName = "Test Enterprise",
            IsDeleted = false
        };

        var enterprises = new List<Enterprise> { enterprise }.AsQueryable();
        _mockContext.Setup(x => x.Enterprises).Returns(DbContextMockHelper.BuildMockDbSet(enterprises).Object);

        var departmentQuery = departments.AsQueryable();
        _mockContext.Setup(x => x.Departments).Returns(DbContextMockHelper.BuildMockDbSet(departmentQuery).Object);

        var employees = new List<Employee>().AsQueryable();
        _mockContext.Setup(x => x.Employees).Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    private static IFormFile CreateMockFormFile(string fileName, string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.Length).Returns(stream.Length);
        fileMock.Setup(f => f.OpenReadStream()).Returns(stream);
        fileMock.Setup(f => f.ContentType).Returns("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        return fileMock.Object;
    }

    private static Mock<UserManager<User>> MockUserManager()
    {
        var store = new Mock<IUserStore<User>>();
        return new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
    }
}
