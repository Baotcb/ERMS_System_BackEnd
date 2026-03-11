using ERMS.Application.Features.Employees.Queries.GetEmployeeById;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Organization;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace ERMS.UnitTests.Features.Employees.Queries.GetEmployeeById;

public class GetEmployeeByIdHandlerTest
{
    private readonly Mock<IERMSDbContext> _mockContext;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<UserManager<User>> _mockUserManager;
    private readonly GetEmployeeByIdHandler _handler;

    public GetEmployeeByIdHandlerTest()
    {
        _mockContext = new Mock<IERMSDbContext>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockUserManager = MockUserManager();

        _handler = new GetEmployeeByIdHandler(
            _mockContext.Object,
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
        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync((Guid?)null);

        var query = new GetEmployeeByIdQuery { Id = Guid.NewGuid() };

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(query, CancellationToken.None));

        exception.Message.Should().Be("Người dùng không thuộc doanh nghiệp nào.");
    }

    [Fact]
    public async Task Handle_WhenEmployeeNotFound_ThrowsKeyNotFoundException()
    {
        var enterpriseId = Guid.NewGuid();
        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        var employees = new List<Employee>().AsQueryable();
        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        var query = new GetEmployeeByIdQuery { Id = Guid.NewGuid() };

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(query, CancellationToken.None));

        exception.Message.Should().Be("Nhân viên không tồn tại");
    }

    [Fact]
    public async Task Handle_WhenEmployeeBelongsToDifferentEnterprise_ThrowsKeyNotFoundException()
    {
        var enterpriseId = Guid.NewGuid();
        var otherEnterpriseId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        var employees = new List<Employee>
        {
            new()
            {
                Id = employeeId,
                EnterpriseId = otherEnterpriseId,
                IsDeleted = false,
                User = new User { FullName = "Other User", Email = "other@example.com" },
                Department = new Department { DepartmentName = "Other Department" }
            }
        }.AsQueryable();

        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        var query = new GetEmployeeByIdQuery { Id = employeeId };

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(query, CancellationToken.None));

        exception.Message.Should().Be("Nhân viên không tồn tại");
    }

    [Fact]
    public async Task Handle_WhenEmployeeExists_ReturnsEmployeeDetails()
    {
        var enterpriseId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var departmentId = 10;
        var createdAt = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var updatedAt = new DateTime(2024, 2, 3, 4, 5, 6, DateTimeKind.Utc);
        var hireDate = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        var managerUser = new User
        {
            Id = managerId,
            FullName = "Jane Manager",
            Email = "manager@example.com"
        };

        var manager = new Employee
        {
            Id = managerId,
            UserId = managerId,
            EnterpriseId = enterpriseId,
            EmployeeCode = "EMP-MGR",
            User = managerUser,
            Department = new Department { Id = 11, DepartmentName = "Management" },
            EmploymentType = "FullTime",
            Status = "Active"
        };

        var employee = new Employee
        {
            Id = employeeId,
            UserId = userId,
            EnterpriseId = enterpriseId,
            EmployeeCode = "EMP001",
            DepartmentId = departmentId,
            Position = "Senior Developer",
            JobPositionId = Guid.NewGuid(),
            HireDate = hireDate,
            EmploymentType = "FullTime",
            ManagerId = managerId,
            Manager = manager,
            Salary = 2500m,
            IsTrainer = true,
            Status = "Active",
            IsDeleted = false,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            User = new User
            {
                Id = userId,
                FullName = "John Employee",
                Email = "john@example.com",
                PhoneNumber = "0123456789"
            },
            Department = new Department
            {
                Id = departmentId,
                DepartmentName = "Engineering"
            }
        };

        var employees = new List<Employee> { employee }.AsQueryable();
        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        // Mock UserManager để trả về roles
        var userEntity = employee.User;
        _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(userEntity);
        _mockUserManager.Setup(x => x.GetRolesAsync(userEntity))
            .ReturnsAsync(new List<string> { "Employee", "Trainer" });

        var result = await _handler.Handle(new GetEmployeeByIdQuery { Id = employeeId }, CancellationToken.None);

        result.Id.Should().Be(employeeId);
        result.UserId.Should().Be(userId);
        result.EmployeeCode.Should().Be("EMP001");
        result.FullName.Should().Be("John Employee");
        result.Email.Should().Be("john@example.com");
        result.Phone.Should().Be("0123456789");
        result.DepartmentId.Should().Be(departmentId);
        result.DepartmentName.Should().Be("Engineering");
        result.Position.Should().Be("Senior Developer");
        result.JobPositionId.Should().Be(employee.JobPositionId);
        result.HireDate.Should().Be(hireDate);
        result.EmploymentType.Should().Be("FullTime");
        result.ManagerId.Should().Be(managerId);
        result.ManagerName.Should().Be("Jane Manager");
        result.Salary.Should().Be(2500m);
        result.IsTrainer.Should().BeTrue();
        result.Status.Should().Be("Active");
        result.CreatedAt.Should().Be(createdAt);
        result.UpdatedAt.Should().Be(updatedAt);
        result.Roles.Should().Contain("Employee");
        result.Roles.Should().Contain("Trainer");
    }
}
