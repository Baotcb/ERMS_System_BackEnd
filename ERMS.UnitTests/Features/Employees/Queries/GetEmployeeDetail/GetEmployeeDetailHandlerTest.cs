using ERMS.Application.Features.Employees.Queries.GetEmployeeDetail;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Organization;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace ERMS.UnitTests.Features.Employees.Queries.GetEmployeeDetail;

public class GetEmployeeDetailHandlerTest
{
    private readonly Mock<IERMSDbContext> _mockContext;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly GetEmployeeDetailHandler _handler;

    public GetEmployeeDetailHandlerTest()
    {
        _mockContext = new Mock<IERMSDbContext>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();

        _handler = new GetEmployeeDetailHandler(
            _mockContext.Object,
            _mockCurrentUserService.Object
        );
    }

    [Fact]
    public async Task Handle_WhenEmployeeExists_ReturnsSkillDescription()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var departmentId = 1;

        _mockCurrentUserService.Setup(x => x.GetEnterpriseIdAsync())
            .ReturnsAsync(enterpriseId);

        var department = new Department
        {
            Id = departmentId,
            DepartmentName = "IT Department"
        };

        var employee = new Employee
        {
            Id = employeeId,
            EnterpriseId = enterpriseId,
            DepartmentId = departmentId,
            EmployeeCode = "EMP001",
            Position = "Senior Developer",
            EmploymentType = "FullTime",
            HireDate = new DateTime(2024, 1, 15),
            Status = "Active",
            CreatedAt = new DateTime(2024, 1, 1),
            ManagerId = Guid.NewGuid(),
            SkillDescription = "Kỹ năng phù hợp: C#, .NET",
            IsDeleted = false,
            User = new User
            {
                FullName = "John Doe",
                Email = "john@example.com",
                PhoneNumber = "0123456789"
            },
            Department = department
        };

        var employees = new List<Employee> { employee }.AsQueryable();
        _mockContext.Setup(x => x.Employees)
            .Returns(DbContextMockHelper.BuildMockDbSet(employees).Object);

        // Act
        var result = await _handler.Handle(new GetEmployeeDetailQuery { Id = employeeId }, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.SkillDescription.Should().Be("Kỹ năng phù hợp: C#, .NET");
        result.FullName.Should().Be("John Doe");
        result.DepartmentName.Should().Be("IT Department");
    }
}
