using ERMS.Application.Features.Employees.Queries.GetAllEmployees;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Organization;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace ERMS.UnitTests.Features.Employees.Queries.GetAllEmployees;

public class GetAllEmployeesHandlerTest
{
    private readonly Mock<IERMSDbContext> _mockContext;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly GetAllEmployeesHandler _handler;

    public GetAllEmployeesHandlerTest()
    {
        _mockContext = new Mock<IERMSDbContext>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();

        _handler = new GetAllEmployeesHandler(
            _mockContext.Object,
            _mockCurrentUserService.Object
        );
    }

    [Fact]
    public async Task Handle_WhenUserNotBelongToEnterprise_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync((Guid?)null);

        var query = new GetAllEmployeesQuery();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(query, CancellationToken.None));

        exception.Message.Should().Be("Người dùng không thuộc doanh nghiệp nào.");
    }

    [Fact]
    public async Task Handle_WhenNoEmployees_ReturnsEmptyResult()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        var employees = new List<Employee>().AsQueryable();
        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        var query = new GetAllEmployeesQuery { Page = 1, PageSize = 10 };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenEmployeesExist_ReturnsEmployees()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var departmentId = 1;

        var department = new Department
        {
            Id = departmentId,
            DepartmentName = "IT Department"
        };

        var user = new User
        {
            FullName = "John Doe",
            Email = "john@example.com",
            PhoneNumber = "0123456789"
        };

        var employees = new List<Employee>
        {
            new Employee
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                DepartmentId = departmentId,
                EmployeeCode = "EMP001",
                Position = "Developer",
                EmploymentType = "FullTime",
                HireDate = DateTime.UtcNow,
                Status = "Active",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                User = user,
                Department = department
            }
        }.AsQueryable();

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        var query = new GetAllEmployeesQuery { Page = 1, PageSize = 10 };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        result.Items[0].EmployeeCode.Should().Be("EMP001");
        result.Items[0].FullName.Should().Be("John Doe");
        result.Items[0].Email.Should().Be("john@example.com");
        result.Items[0].DepartmentName.Should().Be("IT Department");
    }

    [Fact]
    public async Task Handle_ExcludesDeletedEmployees()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var departmentId = 1;

        var department = new Department
        {
            Id = departmentId,
            DepartmentName = "IT"
        };

        var employees = new List<Employee>
        {
            new Employee
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                DepartmentId = departmentId,
                IsDeleted = false,
                User = new User { FullName = "Active Employee", Email = "active@example.com" },
                Department = department,
                EmployeeCode = "EMP001",
                EmploymentType = "FullTime",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                DepartmentId = departmentId,
                IsDeleted = true,
                User = new User { FullName = "Deleted Employee", Email = "deleted@example.com" },
                Department = department,
                EmployeeCode = "EMP002",
                EmploymentType = "FullTime",
                Status = "Inactive",
                CreatedAt = DateTime.UtcNow
            }
        }.AsQueryable();

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        var query = new GetAllEmployeesQuery { Page = 1, PageSize = 10 };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].FullName.Should().Be("Active Employee");
    }

    [Fact]
    public async Task Handle_WhenSearchProvided_FiltersEmployeesBySearch()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var departmentId = 1;

        var department = new Department
        {
            Id = departmentId,
            DepartmentName = "IT"
        };

        var employees = new List<Employee>
        {
            new Employee
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                DepartmentId = departmentId,
                EmployeeCode = "EMP001",
                IsDeleted = false,
                User = new User { FullName = "John Doe", Email = "john@example.com" },
                Department = department,
                EmploymentType = "FullTime",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                DepartmentId = departmentId,
                EmployeeCode = "EMP002",
                IsDeleted = false,
                User = new User { FullName = "Jane Smith", Email = "jane@example.com" },
                Department = department,
                EmploymentType = "FullTime",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            }
        }.AsQueryable();

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        var query = new GetAllEmployeesQuery
        {
            Page = 1,
            PageSize = 10,
            Search = "John"
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].FullName.Should().Be("John Doe");
    }

    [Fact]
    public async Task Handle_WhenSearchByEmail_FiltersCorrectly()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var departmentId = 1;

        var department = new Department
        {
            Id = departmentId,
            DepartmentName = "IT"
        };

        var employees = new List<Employee>
        {
            new Employee
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                DepartmentId = departmentId,
                EmployeeCode = "EMP001",
                IsDeleted = false,
                User = new User { FullName = "John Doe", Email = "john.doe@example.com" },
                Department = department,
                EmploymentType = "FullTime",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                DepartmentId = departmentId,
                EmployeeCode = "EMP002",
                IsDeleted = false,
                User = new User { FullName = "Jane Smith", Email = "jane.smith@example.com" },
                Department = department,
                EmploymentType = "FullTime",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            }
        }.AsQueryable();

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        var query = new GetAllEmployeesQuery
        {
            Page = 1,
            PageSize = 10,
            Search = "jane.smith"
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Email.Should().Be("jane.smith@example.com");
    }

    [Fact]
    public async Task Handle_WhenSearchByEmployeeCode_FiltersCorrectly()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var departmentId = 1;

        var department = new Department
        {
            Id = departmentId,
            DepartmentName = "IT"
        };

        var employees = new List<Employee>
        {
            new Employee
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                DepartmentId = departmentId,
                EmployeeCode = "EMP001",
                IsDeleted = false,
                User = new User { FullName = "John Doe", Email = "john@example.com" },
                Department = department,
                EmploymentType = "FullTime",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                DepartmentId = departmentId,
                EmployeeCode = "EMP002",
                IsDeleted = false,
                User = new User { FullName = "Jane Smith", Email = "jane@example.com" },
                Department = department,
                EmploymentType = "FullTime",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            }
        }.AsQueryable();

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        var query = new GetAllEmployeesQuery
        {
            Page = 1,
            PageSize = 10,
            Search = "EMP002"
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].EmployeeCode.Should().Be("EMP002");
    }

    [Fact]
    public async Task Handle_WhenDepartmentIdProvided_FiltersEmployeesByDepartment()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var departmentId1 = 1;
        var departmentId2 = 2;

        var department1 = new Department { Id = departmentId1, DepartmentName = "IT" };
        var department2 = new Department { Id = departmentId2, DepartmentName = "HR" };

        var employees = new List<Employee>
        {
            new Employee
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                DepartmentId = departmentId1,
                IsDeleted = false,
                User = new User { FullName = "John Doe", Email = "john@example.com" },
                Department = department1,
                EmployeeCode = "EMP001",
                EmploymentType = "FullTime",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                DepartmentId = departmentId2,
                IsDeleted = false,
                User = new User { FullName = "Jane Smith", Email = "jane@example.com" },
                Department = department2,
                EmployeeCode = "EMP002",
                EmploymentType = "FullTime",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            }
        }.AsQueryable();

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        var query = new GetAllEmployeesQuery
        {
            Page = 1,
            PageSize = 10,
            DepartmentId = departmentId1
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].DepartmentId.Should().Be(departmentId1);
        result.Items[0].DepartmentName.Should().Be("IT");
    }

    [Fact]
    public async Task Handle_WhenStatusProvided_FiltersEmployeesByStatus()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var departmentId = 1;

        var department = new Department { Id = departmentId, DepartmentName = "IT" };

        var employees = new List<Employee>
        {
            new Employee
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                DepartmentId = departmentId,
                Status = "Active",
                IsDeleted = false,
                User = new User { FullName = "John Doe", Email = "john@example.com" },
                Department = department,
                EmployeeCode = "EMP001",
                EmploymentType = "FullTime",
                CreatedAt = DateTime.UtcNow
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                DepartmentId = departmentId,
                Status = "OnLeave",
                IsDeleted = false,
                User = new User { FullName = "Jane Smith", Email = "jane@example.com" },
                Department = department,
                EmployeeCode = "EMP002",
                EmploymentType = "FullTime",
                CreatedAt = DateTime.UtcNow
            }
        }.AsQueryable();

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        var query = new GetAllEmployeesQuery
        {
            Page = 1,
            PageSize = 10,
            Status = "Active"
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Status.Should().Be("Active");
    }

    [Fact]
    public async Task Handle_WhenMultipleFiltersProvided_AppliesAllFilters()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var departmentId = 1;

        var department = new Department { Id = departmentId, DepartmentName = "IT" };

        var employees = new List<Employee>
        {
            new Employee
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                DepartmentId = departmentId,
                Status = "Active",
                IsDeleted = false,
                User = new User { FullName = "John Doe", Email = "john@example.com" },
                Department = department,
                EmployeeCode = "EMP001",
                EmploymentType = "FullTime",
                CreatedAt = DateTime.UtcNow
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                DepartmentId = departmentId,
                Status = "OnLeave",
                IsDeleted = false,
                User = new User { FullName = "John Smith", Email = "johnsmith@example.com" },
                Department = department,
                EmployeeCode = "EMP002",
                EmploymentType = "FullTime",
                CreatedAt = DateTime.UtcNow
            }
        }.AsQueryable();

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        var query = new GetAllEmployeesQuery
        {
            Page = 1,
            PageSize = 10,
            Search = "John",
            DepartmentId = departmentId,
            Status = "Active"
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].FullName.Should().Be("John Doe");
        result.Items[0].Status.Should().Be("Active");
    }

    [Fact]
    public async Task Handle_WhenPagingRequested_ReturnsCorrectPage()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var departmentId = 1;

        var department = new Department { Id = departmentId, DepartmentName = "IT" };

        var employees = new List<Employee>();
        for (int i = 0; i < 15; i++)
        {
            employees.Add(new Employee
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                DepartmentId = departmentId,
                IsDeleted = false,
                User = new User
                {
                    FullName = $"Employee {i}",
                    Email = $"emp{i}@example.com"
                },
                Department = department,
                EmployeeCode = $"EMP{i:D3}",
                EmploymentType = "FullTime",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            });
        }

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees.AsQueryable()).Object);

        var query = new GetAllEmployeesQuery
        {
            Page = 2,
            PageSize = 10
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(5);
        result.TotalCount.Should().Be(15);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task Handle_OrdersByFullNameAscending()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var departmentId = 1;

        var department = new Department { Id = departmentId, DepartmentName = "IT" };

        var employees = new List<Employee>
        {
            new Employee
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                DepartmentId = departmentId,
                IsDeleted = false,
                User = new User { FullName = "Charlie", Email = "charlie@example.com" },
                Department = department,
                EmployeeCode = "EMP001",
                EmploymentType = "FullTime",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                DepartmentId = departmentId,
                IsDeleted = false,
                User = new User { FullName = "Alice", Email = "alice@example.com" },
                Department = department,
                EmployeeCode = "EMP002",
                EmploymentType = "FullTime",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                DepartmentId = departmentId,
                IsDeleted = false,
                User = new User { FullName = "Bob", Email = "bob@example.com" },
                Department = department,
                EmployeeCode = "EMP003",
                EmploymentType = "FullTime",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            }
        }.AsQueryable();

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        var query = new GetAllEmployeesQuery { Page = 1, PageSize = 10 };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].FullName.Should().Be("Alice");
        result.Items[1].FullName.Should().Be("Bob");
        result.Items[2].FullName.Should().Be("Charlie");
    }

    [Fact]
    public async Task Handle_OnlyReturnsEmployeesFromCurrentEnterprise()
    {
        // Arrange
        var enterpriseId1 = Guid.NewGuid();
        var enterpriseId2 = Guid.NewGuid();
        var departmentId = 1;

        var department = new Department { Id = departmentId, DepartmentName = "IT" };

        var employees = new List<Employee>
        {
            new Employee
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId1,
                DepartmentId = departmentId,
                IsDeleted = false,
                User = new User { FullName = "John from Enterprise 1", Email = "john@ent1.com" },
                Department = department,
                EmployeeCode = "EMP001",
                EmploymentType = "FullTime",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId2,
                DepartmentId = departmentId,
                IsDeleted = false,
                User = new User { FullName = "Jane from Enterprise 2", Email = "jane@ent2.com" },
                Department = department,
                EmployeeCode = "EMP002",
                EmploymentType = "FullTime",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            }
        }.AsQueryable();

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId1);

        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        var query = new GetAllEmployeesQuery { Page = 1, PageSize = 10 };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Email.Should().Be("john@ent1.com");
    }

    [Fact]
    public async Task Handle_MapsAllPropertiesToDto()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var departmentId = 1;
        var hireDate = new DateTime(2024, 1, 15);
        var createdAt = new DateTime(2024, 1, 1);

        var department = new Department
        {
            Id = departmentId,
            DepartmentName = "IT Department"
        };

        var user = new User
        {
            FullName = "John Doe",
            Email = "john@example.com",
            PhoneNumber = "0123456789"
        };

        var employees = new List<Employee>
        {
            new Employee
            {
                Id = employeeId,
                EnterpriseId = enterpriseId,
                DepartmentId = departmentId,
                EmployeeCode = "EMP001",
                Position = "Senior Developer",
                EmploymentType = "FullTime",
                HireDate = hireDate,
                SkillDescription = "C#, .NET, Azure",
                Status = "Active",
                IsDeleted = false,
                CreatedAt = createdAt,
                User = user,
                Department = department
            }
        }.AsQueryable();

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        var query = new GetAllEmployeesQuery { Page = 1, PageSize = 10 };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        var dto = result.Items[0];
        dto.Id.Should().Be(employeeId);
        dto.EmployeeCode.Should().Be("EMP001");
        dto.FullName.Should().Be("John Doe");
        dto.Email.Should().Be("john@example.com");
        dto.Phone.Should().Be("0123456789");
        dto.DepartmentId.Should().Be(departmentId);
        dto.DepartmentName.Should().Be("IT Department");
        dto.Position.Should().Be("Senior Developer");
        dto.EmploymentType.Should().Be("FullTime");
        dto.HireDate.Should().Be(hireDate);
        dto.Status.Should().Be("Active");
        dto.CreatedAt.Should().Be(createdAt);
        dto.SkillDescription.Should().Be("C#, .NET, Azure");
    }

    [Fact]
    public async Task Handle_WhenSearchIsCaseInsensitive_FiltersCorrectly()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var departmentId = 1;

        var department = new Department { Id = departmentId, DepartmentName = "IT" };

        var employees = new List<Employee>
        {
            new Employee
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                DepartmentId = departmentId,
                IsDeleted = false,
                User = new User { FullName = "JOHN DOE", Email = "JOHN@EXAMPLE.COM" },
                Department = department,
                EmployeeCode = "EMP001",
                EmploymentType = "FullTime",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            }
        }.AsQueryable();

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        var query = new GetAllEmployeesQuery
        {
            Page = 1,
            PageSize = 10,
            Search = "john doe"
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].FullName.Should().Be("JOHN DOE");
    }
}
