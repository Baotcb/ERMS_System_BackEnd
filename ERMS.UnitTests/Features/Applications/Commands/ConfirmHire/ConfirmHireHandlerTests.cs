using ERMS.Application.Features.Applications.Commands.ConfirmHire;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Candidate;
using ERMS.Domain.Entities.Application;
using ERMS.Domain.Entities.Enterprise;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Organization;
using ERMS.Domain.Entities.Recruitment;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;

using ApplicationEntity = ERMS.Domain.Entities.Application.Application;
using OfferEntity = ERMS.Domain.Entities.Application.Offer;

namespace ERMS.UnitTests.Features.Applications.Commands.ConfirmHire;

public class ConfirmHireHandlerTests
{
    private readonly Mock<IERMSDbContext> _contextMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<ILogger<ConfirmHireHandler>> _loggerMock;
    private readonly ConfirmHireHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _applicationId = Guid.NewGuid();
    private readonly Guid _candidateId = Guid.NewGuid();
    private readonly Guid _candidateUserId = Guid.NewGuid();
    private readonly Guid _enterpriseId = Guid.NewGuid();
    private readonly Guid _jobPostingId = Guid.NewGuid();
    private const string TestEmail = "john.doe@company.com";

    public ConfirmHireHandlerTests()
    {
        _contextMock = new Mock<IERMSDbContext>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _emailServiceMock = new Mock<IEmailService>();
        _loggerMock = new Mock<ILogger<ConfirmHireHandler>>();

        // Mock UserManager
        var store = new Mock<IUserStore<User>>();
        _userManagerMock = new Mock<UserManager<User>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Mock transaction
        var transactionMock = new Mock<IDbContextTransaction>();
        _contextMock.Setup(c => c.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionMock.Object);

        _handler = new ConfirmHireHandler(
            _contextMock.Object,
            _currentUserServiceMock.Object,
            _userManagerMock.Object,
            _emailServiceMock.Object,
            _loggerMock.Object);
    }

    #region Helpers

    private static Mock<DbSet<T>> CreateMockDbSet<T>(IQueryable<T> data) where T : class
    {
        var mockSet = new Mock<DbSet<T>>();

        mockSet.As<IAsyncEnumerable<T>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<T>(data.GetEnumerator()));

        mockSet.As<IQueryable<T>>()
            .Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<T>(data.Provider));

        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());

        return mockSet;
    }

    private void SetupAuthenticatedHR()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(x => x.Roles).Returns([AppRoles.HRManager]);
        _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(_enterpriseId);
    }

    private ConfirmHireCommand CreateValidCommand()
    {
        return new ConfirmHireCommand
        {
            ApplicationId = _applicationId,
            EmployeeEmail = TestEmail
        };
    }

    private ApplicationEntity CreateApplication(
        string stage = "Offered",
        string offerStatus = "Accepted",
        Guid? enterpriseId = null,
        bool includeOffer = true,
        CVScreeningResult? screeningResult = null)
    {
        var candidate = new Candidate
        {
            Id = _candidateId,
            UserId = _candidateUserId,
            User = new User
            {
                Id = _candidateUserId,
                FullName = "John Doe",
                Email = "john.candidate@gmail.com",
                PhoneNumber = "0123456789"
            },
            IsDeleted = false
        };

        var application = new ApplicationEntity
        {
            Id = _applicationId,
            JobPostingId = _jobPostingId,
            CandidateId = _candidateId,
            Candidate = candidate,
            Stage = stage,
            Status = "Active",
            AppliedAt = DateTime.UtcNow.AddDays(-30),
            IsDeleted = false,
            JobPosting = new JobPosting
            {
                Id = _jobPostingId,
                EnterpriseId = enterpriseId ?? _enterpriseId,
                DepartmentId = 1,
                JobTitle = "Software Engineer",
                Description = "Some description"
            }
        };

        if (includeOffer)
        {
            application.Offer = new OfferEntity
            {
                Id = Guid.NewGuid(),
                ApplicationId = _applicationId,
                Position = "Software Engineer",
                DepartmentId = 1,
                Salary = 5000,
                SalaryFrequency = "Monthly",
                StartDate = DateTime.UtcNow.AddMonths(1),
                ExpirationDate = DateTime.UtcNow.AddDays(7),
                Status = offerStatus,
                CreatedById = Guid.NewGuid(),
                IsDeleted = false
            };
        }

        application.CVScreeningResult = screeningResult;

        return application;
    }

    private void SetupApplicationDbSet(ApplicationEntity? application)
    {
        var data = application != null
            ? new List<ApplicationEntity> { application }.AsQueryable()
            : new List<ApplicationEntity>().AsQueryable();

        var mockSet = CreateMockDbSet(data);
        _contextMock.Setup(c => c.Applications).Returns(mockSet.Object);
    }

    private void SetupEnterpriseDbSet()
    {
        var enterprise = new Enterprise
        {
            Id = _enterpriseId,
            EnterpriseName = "Test Corp",
            EnterpriseCode = "TC",
            IsDeleted = false
        };
        var data = new List<Enterprise> { enterprise }.AsQueryable();
        var mockSet = CreateMockDbSet(data);
        _contextMock.Setup(c => c.Enterprises).Returns(mockSet.Object);
    }

    private void SetupEmployeesDbSet(int existingCount = 5)
    {
        var employees = Enumerable.Range(0, existingCount)
            .Select(i => new Employee
            {
                Id = Guid.NewGuid(),
                EnterpriseId = _enterpriseId,
                EmployeeCode = $"TC-{i + 1:D4}",
                UserId = Guid.NewGuid(),
                DepartmentId = 1
            }).ToList();
        var data = employees.AsQueryable();
        var mockSet = CreateMockDbSet(data);
        _contextMock.Setup(c => c.Employees).Returns(mockSet.Object);
    }

    private void SetupUserManagerSuccess()
    {
        _userManagerMock.Setup(x => x.FindByEmailAsync(TestEmail)).ReturnsAsync((User?)null);
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), AppRoles.Employee))
            .ReturnsAsync(IdentityResult.Success);
    }

    private void SetupFullHappyPath()
    {
        SetupAuthenticatedHR();
        SetupApplicationDbSet(CreateApplication());
        SetupEnterpriseDbSet();
        SetupEmployeesDbSet();
        SetupUserManagerSuccess();
    }

    #endregion

    #region Security & Authorization Tests

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserNotAuthenticated()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);
        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Người dùng chưa được xác thực.");
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserIsNotHRManager()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(x => x.Roles).Returns([AppRoles.Candidate]);
        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*HR Manager*");
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenNoEnterprise()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(x => x.Roles).Returns([AppRoles.HRManager]);
        _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync((Guid?)null);
        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*doanh nghiệp*");
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenEnterpriseMismatch()
    {
        // Arrange
        SetupAuthenticatedHR();
        var otherEnterpriseId = Guid.NewGuid();
        SetupApplicationDbSet(CreateApplication(enterpriseId: otherEnterpriseId));
        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*quyền*");
    }

    #endregion

    #region Not Found Tests

    [Fact]
    public async Task Handle_ShouldThrowException_WhenApplicationNotFound()
    {
        // Arrange
        SetupAuthenticatedHR();
        SetupApplicationDbSet(null);
        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("*Không tìm thấy hồ sơ ứng tuyển**");
    }

    #endregion

    #region Business Rule Violations

    [Fact]
    public async Task Handle_ShouldThrowException_WhenOfferNotAccepted()
    {
        // Arrange
        SetupAuthenticatedHR();
        SetupApplicationDbSet(CreateApplication(offerStatus: OfferStatus.Sent));
        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("*Không thể xác nhận tuyển dụng*Accepted*");
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenNoOffer()
    {
        // Arrange
        SetupAuthenticatedHR();
        SetupApplicationDbSet(CreateApplication(includeOffer: false));
        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("*không có đề nghị đang hoạt động*");
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenAlreadyHired()
    {
        // Arrange
        SetupAuthenticatedHR();
        SetupApplicationDbSet(CreateApplication(stage: ApplicationStage.Hired));
        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("*đã được xác nhận tuyển dụng*");
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenEmailAlreadyExists()
    {
        // Arrange
        SetupAuthenticatedHR();
        SetupApplicationDbSet(CreateApplication());
        _userManagerMock.Setup(x => x.FindByEmailAsync(TestEmail))
            .ReturnsAsync(new User { Email = TestEmail });
        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("*Email đã được sử dụng*");
    }

    #endregion

    #region Happy Path Tests

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenAllValidationsPass()
    {
        // Arrange
        SetupFullHappyPath();
        var command = CreateValidCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ApplicationId.Should().Be(_applicationId);
        result.NewStage.Should().Be(ApplicationStage.Hired);
        result.EmployeeEmail.Should().Be(TestEmail);
        result.EmployeeCode.Should().StartWith("TC-");
        result.EmployeeId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldPopulateSkillDescription_FromCvScreeningResult()
    {
        // Arrange
        SetupAuthenticatedHR();
        SetupEnterpriseDbSet();
        SetupEmployeesDbSet();
        SetupUserManagerSuccess();

        var application = CreateApplication(
            screeningResult: new CVScreeningResult
            {
                MatchedSkills = "[\"C#\",\".NET\"]",
                MissingSkills = "[\"Azure\"]"
            });
        SetupApplicationDbSet(application);

        Employee? createdEmployee = null;
        var employeeSet = CreateMockDbSet(new List<Employee>().AsQueryable());
        employeeSet.Setup(x => x.Add(It.IsAny<Employee>()))
            .Callback<Employee>(employee => createdEmployee = employee);
        _contextMock.Setup(x => x.Employees).Returns(employeeSet.Object);

        var command = CreateValidCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.EmployeeId.Should().NotBeEmpty();
        createdEmployee.Should().NotBeNull();
        createdEmployee!.SkillDescription.Should().Contain("Kỹ năng phù hợp");
        createdEmployee.SkillDescription.Should().Contain("C#");
        createdEmployee.SkillDescription.Should().Contain(".NET");
        createdEmployee.SkillDescription.Should().Contain("Kỹ năng còn thiếu");
        createdEmployee.SkillDescription.Should().Contain("Azure");
        createdEmployee.SkillDescription.Should().NotContain("[");
        createdEmployee.SkillDescription.Should().NotContain("]");
    }

    [Fact]
    public async Task Handle_ShouldIgnoreInvalidSkillJson_AndStillCreateEmployee()
    {
        // Arrange
        SetupAuthenticatedHR();
        SetupEnterpriseDbSet();
        SetupEmployeesDbSet();
        SetupUserManagerSuccess();

        var application = CreateApplication(
            screeningResult: new CVScreeningResult
            {
                MatchedSkills = "{not valid json",
                MissingSkills = string.Empty
            });
        SetupApplicationDbSet(application);

        Employee? createdEmployee = null;
        var employeeSet = CreateMockDbSet(new List<Employee>().AsQueryable());
        employeeSet.Setup(x => x.Add(It.IsAny<Employee>()))
            .Callback<Employee>(employee => createdEmployee = employee);
        _contextMock.Setup(x => x.Employees).Returns(employeeSet.Object);

        var command = CreateValidCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.NewStage.Should().Be(ApplicationStage.Hired);
        createdEmployee.Should().NotBeNull();
        createdEmployee!.SkillDescription.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldCreateEmployeeWithRoleAssignment()
    {
        // Arrange
        SetupFullHappyPath();
        var command = CreateValidCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Once);
        _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<User>(), AppRoles.Employee), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldSendEmailWithCredentials()
    {
        // Arrange
        SetupFullHappyPath();
        var command = CreateValidCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _emailServiceMock.Verify(x => x.SendEmailAsync(
            "john.candidate@gmail.com",
            It.IsAny<string>(),
            It.Is<string>(body => body.Contains(TestEmail))),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldStillSucceed_WhenEmailSendingFails()
    {
        // Arrange
        SetupFullHappyPath();
        _emailServiceMock.Setup(x => x.SendEmailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("SMTP error"));

        var command = CreateValidCommand();

        // Act — should NOT throw despite email failure
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.NewStage.Should().Be(ApplicationStage.Hired);
    }

    [Fact]
    public async Task Handle_ShouldSaveChanges()
    {
        // Arrange
        SetupFullHappyPath();
        var command = CreateValidCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
