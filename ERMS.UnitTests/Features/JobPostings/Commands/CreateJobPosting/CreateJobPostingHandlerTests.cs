using ERMS.Application.Features.JobPostings.Commands.CreateJobPosting;
using ERMS.Application.Interface;
using FluentAssertions;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Constants.Recruitment;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Recruitment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;

// Alias to avoid namespace collision with ERMS.Application
using ApplicationEntity = ERMS.Domain.Entities.Application.Application;

namespace ERMS.UnitTests.Features.JobPostings.Commands.CreateJobPosting;

public class CreateJobPostingHandlerTests
{
    private readonly Mock<IERMSDbContext> _contextMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<ILogger<CreateJobPostingHandler>> _loggerMock;
    private readonly CreateJobPostingHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _enterpriseId = Guid.NewGuid();
    private readonly Guid _planDetailId = Guid.NewGuid();
    private readonly Guid _recruitmentPlanId = Guid.NewGuid();

    public CreateJobPostingHandlerTests()
    {
        _contextMock = new Mock<IERMSDbContext>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _loggerMock = new Mock<ILogger<CreateJobPostingHandler>>();

        _handler = new CreateJobPostingHandler(
            _contextMock.Object,
            _currentUserServiceMock.Object,
            _loggerMock.Object);
    }

    #region Helper Methods

    private void SetupCurrentUser()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(x => x.Roles).Returns([AppRoles.HRManager]);
        _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(_enterpriseId);
    }

    private PlanDetail CreateApprovedPlanDetail(int quantity, string? requiredSkills = null)
    {
        return new PlanDetail
        {
            Id = _planDetailId,
            RecruitmentPlanId = _recruitmentPlanId,
            RequestedById = _userId,
            PositionTitle = "Software Engineer",
            Quantity = quantity,
            RequiredSkills = requiredSkills ?? "[\"C#\", \".NET\", \"SQL Server\"]",
            MinExperience = 2,
            MaxExperience = 5,
            SalaryRangeMin = 15000000,
            SalaryRangeMax = 30000000,
            Status = PlanDetailStatus.Approved,
            IsDeleted = false,
            RecruitmentPlan = new RecruitmentPlan
            {
                Id = _recruitmentPlanId,
                EnterpriseId = _enterpriseId,
                DepartmentId = 1,
                PlanName = "Q1 2026 Hiring Plan",
                PlanCode = "RP-001",
                Status = PlanStatus.Approved,
                CreatedById = _userId
            }
        };
    }

    private void SetupFullMocks(PlanDetail planDetail)
    {
        var planDetailsData = new List<PlanDetail> { planDetail }.AsQueryable();
        var planDetailsMockSet = CreateMockDbSet(planDetailsData);
        _contextMock.Setup(c => c.PlanDetails).Returns(planDetailsMockSet.Object);

        var applicationsData = new List<ApplicationEntity>().AsQueryable();
        var applicationsMockSet = CreateMockDbSet(applicationsData);
        _contextMock.Setup(c => c.Applications).Returns(applicationsMockSet.Object);

        var jobPostingsData = new List<JobPosting>().AsQueryable();
        var jobPostingsMockSet = CreateMockDbSet(jobPostingsData);
        _contextMock.Setup(c => c.JobPostings).Returns(jobPostingsMockSet.Object);

        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    private CreateJobPostingCommand CreateValidCommand()
    {
        return new CreateJobPostingCommand
        {
            PlanDetailId = _planDetailId,
            ApplicationDeadline = DateTime.UtcNow.AddMonths(1)
        };
    }

    #endregion

    #region Security Tests

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
        _currentUserServiceMock.Setup(x => x.Roles).Returns([AppRoles.DepartmentHead]); // Wrong role
        _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(_enterpriseId);

        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Chỉ HR Manager mới có quyền tạo tin tuyển dụng.");
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserHasNoRoles()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(x => x.Roles).Returns((List<string>?)null);
        _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(_enterpriseId);

        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Chỉ HR Manager mới có quyền tạo tin tuyển dụng.");
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserHasNoEnterprise()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(x => x.Roles).Returns([AppRoles.HRManager]);
        _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync((Guid?)null);

        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Người dùng không thuộc doanh nghiệp nào.");
    }

    #endregion

    #region PlanDetail Validation Tests

    [Fact]
    public async Task Handle_ShouldThrowException_WhenPlanDetailNotFound()
    {
        // Arrange
        SetupCurrentUser();

        var planDetailsData = new List<PlanDetail>().AsQueryable(); // Empty - no matching PlanDetail
        var planDetailsMockSet = CreateMockDbSet(planDetailsData);
        _contextMock.Setup(c => c.PlanDetails).Returns(planDetailsMockSet.Object);

        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("*Không tìm thấy chi tiết kế hoạch với ID**");
    }

    [Fact]
    public async Task Handle_ShouldThrowBusinessRuleException_WhenRecruitmentPlanNotApproved()
    {
        // Arrange
        SetupCurrentUser();

        var planDetail = CreateApprovedPlanDetail(quantity: 3);
        planDetail.RecruitmentPlan.Status = PlanStatus.Pending; // Parent plan NOT approved

        var planDetailsData = new List<PlanDetail> { planDetail }.AsQueryable();
        var planDetailsMockSet = CreateMockDbSet(planDetailsData);
        _contextMock.Setup(c => c.PlanDetails).Returns(planDetailsMockSet.Object);

        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("*Kế hoạch tuyển dụng phải được Giám đốc phê duyệt*");
    }

    [Fact]
    public async Task Handle_ShouldThrowBusinessRuleException_WhenPlanDetailStatusIsNotApproved()
    {
        // Arrange
        SetupCurrentUser();

        var planDetail = CreateApprovedPlanDetail(quantity: 3);
        planDetail.Status = PlanDetailStatus.Pending; // PlanDetail NOT approved

        var planDetailsData = new List<PlanDetail> { planDetail }.AsQueryable();
        var planDetailsMockSet = CreateMockDbSet(planDetailsData);
        _contextMock.Setup(c => c.PlanDetails).Returns(planDetailsMockSet.Object);

        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("*Trạng thái chi tiết kế hoạch phải là 'Approved'*");
    }

    [Fact]
    public async Task Handle_ShouldThrowBusinessRuleException_WhenRequiredSkillsIsEmpty()
    {
        // Arrange
        SetupCurrentUser();

        var planDetail = CreateApprovedPlanDetail(quantity: 3, requiredSkills: ""); // Empty skills
        SetupFullMocks(planDetail);

        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("*kỹ năng*trống*");
    }

    [Fact]
    public async Task Handle_ShouldThrowBusinessRuleException_WhenRequiredSkillsIsWhitespace()
    {
        // Arrange
        SetupCurrentUser();

        var planDetail = CreateApprovedPlanDetail(quantity: 3, requiredSkills: "   "); // Whitespace
        SetupFullMocks(planDetail);

        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("*kỹ năng*trống*");
    }

    #endregion

    #region Quota Validation Tests

    [Fact]
    public async Task Handle_ShouldThrowBusinessRuleException_WhenPlanQuotaExhausted()
    {
        // Arrange
        SetupCurrentUser();

        var planDetail = CreateApprovedPlanDetail(quantity: 2); // Quota = 2

        var planDetailsData = new List<PlanDetail> { planDetail }.AsQueryable();
        var planDetailsMockSet = CreateMockDbSet(planDetailsData);
        _contextMock.Setup(c => c.PlanDetails).Returns(planDetailsMockSet.Object);

        // Create 2 hired applications (quota exhausted)
        var jobPosting = new JobPosting
        {
            Id = Guid.NewGuid(),
            PlanDetailId = _planDetailId,
            EnterpriseId = _enterpriseId,
            DepartmentId = 1,
            JobTitle = "Software Engineer",
            Description = "Test",
            EmploymentType = "Full-time",
            Status = JobPostingStatus.Published,
            IsDeleted = false
        };

        var applicationsData = new List<ApplicationEntity>
        {
            new() { Id = Guid.NewGuid(), JobPostingId = jobPosting.Id, JobPosting = jobPosting, Stage = ApplicationStage.Hired, IsDeleted = false },
            new() { Id = Guid.NewGuid(), JobPostingId = jobPosting.Id, JobPosting = jobPosting, Stage = ApplicationStage.Hired, IsDeleted = false }
        }.AsQueryable();

        var applicationsMockSet = CreateMockDbSet(applicationsData);
        _contextMock.Setup(c => c.Applications).Returns(applicationsMockSet.Object);

        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("*Hết chỉ tiêu*");
    }

    [Fact]
    public async Task Handle_ShouldSucceed_WhenQuotaHasRemainingSlots()
    {
        // Arrange
        SetupCurrentUser();

        var planDetail = CreateApprovedPlanDetail(quantity: 5); // Quota = 5

        var planDetailsData = new List<PlanDetail> { planDetail }.AsQueryable();
        var planDetailsMockSet = CreateMockDbSet(planDetailsData);
        _contextMock.Setup(c => c.PlanDetails).Returns(planDetailsMockSet.Object);

        // Create 2 hired applications (3 remaining slots)
        var jobPosting = new JobPosting
        {
            Id = Guid.NewGuid(),
            PlanDetailId = _planDetailId,
            EnterpriseId = _enterpriseId,
            DepartmentId = 1,
            JobTitle = "Software Engineer",
            Description = "Test",
            EmploymentType = "Full-time",
            Status = JobPostingStatus.Published,
            IsDeleted = false
        };

        var applicationsData = new List<ApplicationEntity>
        {
            new() { Id = Guid.NewGuid(), JobPostingId = jobPosting.Id, JobPosting = jobPosting, Stage = ApplicationStage.Hired, IsDeleted = false },
            new() { Id = Guid.NewGuid(), JobPostingId = jobPosting.Id, JobPosting = jobPosting, Stage = ApplicationStage.Hired, IsDeleted = false }
        }.AsQueryable();

        var applicationsMockSet = CreateMockDbSet(applicationsData);
        _contextMock.Setup(c => c.Applications).Returns(applicationsMockSet.Object);

        var jobPostingsData = new List<JobPosting>().AsQueryable();
        var jobPostingsMockSet = CreateMockDbSet(jobPostingsData);
        _contextMock.Setup(c => c.JobPostings).Returns(jobPostingsMockSet.Object);

        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = CreateValidCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBe(Guid.Empty);
    }

    #endregion

    #region Auto-Fill Logic Tests

    [Fact]
    public async Task Handle_ShouldCopyRequiredSkillsToRequirementsForAIScanning()
    {
        // Arrange
        SetupCurrentUser();

        var planDetail = CreateApprovedPlanDetail(quantity: 3);
        var expectedRequirements = "[\"C#\", \".NET\", \"SQL Server\"]";

        var planDetailsData = new List<PlanDetail> { planDetail }.AsQueryable();
        var planDetailsMockSet = CreateMockDbSet(planDetailsData);
        _contextMock.Setup(c => c.PlanDetails).Returns(planDetailsMockSet.Object);

        var applicationsData = new List<ApplicationEntity>().AsQueryable();
        var applicationsMockSet = CreateMockDbSet(applicationsData);
        _contextMock.Setup(c => c.Applications).Returns(applicationsMockSet.Object);

        var jobPostingsData = new List<JobPosting>().AsQueryable();
        var jobPostingsMockSet = CreateMockDbSet(jobPostingsData);
        _contextMock.Setup(c => c.JobPostings).Returns(jobPostingsMockSet.Object);

        JobPosting? capturedJobPosting = null;
        jobPostingsMockSet.Setup(m => m.Add(It.IsAny<JobPosting>()))
            .Callback<JobPosting>(jp => capturedJobPosting = jp);

        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = CreateValidCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedJobPosting.Should().NotBeNull();
        capturedJobPosting!.Requirements.Should().Be(expectedRequirements);
        capturedJobPosting.JobTitle.Should().Be(planDetail.PositionTitle);
        capturedJobPosting.Status.Should().Be(JobPostingStatus.Draft);
    }

    [Fact]
    public async Task Handle_ShouldUseTitleOverride_WhenProvided()
    {
        // Arrange
        SetupCurrentUser();

        var planDetail = CreateApprovedPlanDetail(quantity: 3);
        SetupFullMocks(planDetail);

        JobPosting? capturedJobPosting = null;
        var jobPostingsMockSet = CreateMockDbSet(new List<JobPosting>().AsQueryable());
        _contextMock.Setup(c => c.JobPostings).Returns(jobPostingsMockSet.Object);
        jobPostingsMockSet.Setup(m => m.Add(It.IsAny<JobPosting>()))
            .Callback<JobPosting>(jp => capturedJobPosting = jp);

        var command = new CreateJobPostingCommand
        {
            PlanDetailId = _planDetailId,
            ApplicationDeadline = DateTime.UtcNow.AddMonths(1),
            TitleOverride = "   Senior .NET Developer   " // With whitespace
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedJobPosting.Should().NotBeNull();
        capturedJobPosting!.JobTitle.Should().Be("Senior .NET Developer"); // Trimmed
    }

    [Fact]
    public async Task Handle_ShouldUsePositionTitle_WhenTitleOverrideIsEmpty()
    {
        // Arrange
        SetupCurrentUser();

        var planDetail = CreateApprovedPlanDetail(quantity: 3);
        SetupFullMocks(planDetail);

        JobPosting? capturedJobPosting = null;
        var jobPostingsMockSet = CreateMockDbSet(new List<JobPosting>().AsQueryable());
        _contextMock.Setup(c => c.JobPostings).Returns(jobPostingsMockSet.Object);
        jobPostingsMockSet.Setup(m => m.Add(It.IsAny<JobPosting>()))
            .Callback<JobPosting>(jp => capturedJobPosting = jp);

        var command = new CreateJobPostingCommand
        {
            PlanDetailId = _planDetailId,
            ApplicationDeadline = DateTime.UtcNow.AddMonths(1),
            TitleOverride = "   " // Whitespace only
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedJobPosting.Should().NotBeNull();
        capturedJobPosting!.JobTitle.Should().Be("Software Engineer"); // From PlanDetail
    }

    [Fact]
    public async Task Handle_ShouldUseDescriptionOverride_WhenProvided()
    {
        // Arrange
        SetupCurrentUser();

        var planDetail = CreateApprovedPlanDetail(quantity: 3);
        SetupFullMocks(planDetail);

        JobPosting? capturedJobPosting = null;
        var jobPostingsMockSet = CreateMockDbSet(new List<JobPosting>().AsQueryable());
        _contextMock.Setup(c => c.JobPostings).Returns(jobPostingsMockSet.Object);
        jobPostingsMockSet.Setup(m => m.Add(It.IsAny<JobPosting>()))
            .Callback<JobPosting>(jp => capturedJobPosting = jp);

        var command = new CreateJobPostingCommand
        {
            PlanDetailId = _planDetailId,
            ApplicationDeadline = DateTime.UtcNow.AddMonths(1),
            DescriptionOverride = "  Custom job description here  "
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedJobPosting.Should().NotBeNull();
        capturedJobPosting!.Description.Should().Be("Custom job description here"); // Trimmed
    }

    [Fact]
    public async Task Handle_ShouldGenerateDefaultDescription_WhenDescriptionOverrideIsEmpty()
    {
        // Arrange
        SetupCurrentUser();

        var planDetail = CreateApprovedPlanDetail(quantity: 3);
        SetupFullMocks(planDetail);

        JobPosting? capturedJobPosting = null;
        var jobPostingsMockSet = CreateMockDbSet(new List<JobPosting>().AsQueryable());
        _contextMock.Setup(c => c.JobPostings).Returns(jobPostingsMockSet.Object);
        jobPostingsMockSet.Setup(m => m.Add(It.IsAny<JobPosting>()))
            .Callback<JobPosting>(jp => capturedJobPosting = jp);

        var command = CreateValidCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedJobPosting.Should().NotBeNull();
        capturedJobPosting!.Description.Should().Be("We are looking for a Software Engineer.");
    }

    [Fact]
    public async Task Handle_ShouldSetDefaultValuesCorrectly()
    {
        // Arrange
        SetupCurrentUser();

        var planDetail = CreateApprovedPlanDetail(quantity: 3);
        SetupFullMocks(planDetail);

        JobPosting? capturedJobPosting = null;
        var jobPostingsMockSet = CreateMockDbSet(new List<JobPosting>().AsQueryable());
        _contextMock.Setup(c => c.JobPostings).Returns(jobPostingsMockSet.Object);
        jobPostingsMockSet.Setup(m => m.Add(It.IsAny<JobPosting>()))
            .Callback<JobPosting>(jp => capturedJobPosting = jp);

        var command = CreateValidCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedJobPosting.Should().NotBeNull();
        capturedJobPosting!.Status.Should().Be(JobPostingStatus.Draft);
        capturedJobPosting.EmploymentType.Should().Be("Full-time");
        capturedJobPosting.ShowSalary.Should().BeTrue();
        capturedJobPosting.ViewCount.Should().Be(0);
        capturedJobPosting.ApplicationCount.Should().Be(0);
        capturedJobPosting.IsDeleted.Should().BeFalse();
        capturedJobPosting.CreatedById.Should().Be(_userId);
        capturedJobPosting.EnterpriseId.Should().Be(_enterpriseId);
        capturedJobPosting.PlanDetailId.Should().Be(_planDetailId);
        capturedJobPosting.DepartmentId.Should().Be(1); // From RecruitmentPlan
    }

    [Fact]
    public async Task Handle_ShouldCopySalaryRangeFromPlanDetail()
    {
        // Arrange
        SetupCurrentUser();

        var planDetail = CreateApprovedPlanDetail(quantity: 3);
        SetupFullMocks(planDetail);

        JobPosting? capturedJobPosting = null;
        var jobPostingsMockSet = CreateMockDbSet(new List<JobPosting>().AsQueryable());
        _contextMock.Setup(c => c.JobPostings).Returns(jobPostingsMockSet.Object);
        jobPostingsMockSet.Setup(m => m.Add(It.IsAny<JobPosting>()))
            .Callback<JobPosting>(jp => capturedJobPosting = jp);

        var command = CreateValidCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedJobPosting.Should().NotBeNull();
        capturedJobPosting!.SalaryRangeMin.Should().Be(15000000);
        capturedJobPosting.SalaryRangeMax.Should().Be(30000000);
        capturedJobPosting.ExperienceLevel.Should().Be("2-5 years");
        capturedJobPosting.Quantity.Should().Be(3);
    }

    #endregion

    #region Helper Methods for Mocking

    private static Mock<DbSet<T>> CreateMockDbSet<T>(IQueryable<T> data) where T : class
    {
        var mockSet = new Mock<DbSet<T>>();
        mockSet.As<IAsyncEnumerable<T>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<T>(data.GetEnumerator()));
        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<T>(data.Provider));
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
        return mockSet;
    }

    #endregion
}

// Helper classes for Async Query Provider (Reused locally to avoid dependencies)
internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    internal TestAsyncQueryProvider(IQueryProvider inner)
    {
        _inner = inner;
    }

    public IQueryable CreateQuery(System.Linq.Expressions.Expression expression)
    {
        return new TestAsyncEnumerable<TEntity>(expression);
    }

    public IQueryable<TElement> CreateQuery<TElement>(System.Linq.Expressions.Expression expression)
    {
        return new TestAsyncEnumerable<TElement>(expression);
    }

    public object Execute(System.Linq.Expressions.Expression expression)
    {
        return _inner.Execute(expression);
    }

    public TResult Execute<TResult>(System.Linq.Expressions.Expression expression)
    {
        return _inner.Execute<TResult>(expression);
    }

    public TResult ExecuteAsync<TResult>(System.Linq.Expressions.Expression expression, CancellationToken cancellationToken)
    {
        var expectedResultType = typeof(TResult).GetGenericArguments()[0];
        var executionResult = typeof(IQueryProvider)
            .GetMethod(
                name: nameof(IQueryProvider.Execute),
                genericParameterCount: 1,
                types: new[] { typeof(System.Linq.Expressions.Expression) })
            .MakeGenericMethod(expectedResultType)
            .Invoke(this, new[] { expression });

        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))
            .MakeGenericMethod(expectedResultType)
            .Invoke(null, new[] { executionResult });
    }
}

internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable)
        : base(enumerable)
    { }

    public TestAsyncEnumerable(System.Linq.Expressions.Expression expression)
        : base(expression)
    { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }

    IQueryProvider IQueryable.Provider
    {
        get { return new TestAsyncQueryProvider<T>(this); }
    }
}

internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;

    public TestAsyncEnumerator(IEnumerator<T> inner)
    {
        _inner = inner;
    }

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> MoveNextAsync()
    {
        return new ValueTask<bool>(_inner.MoveNext());
    }

    public T Current
    {
        get { return _inner.Current; }
    }
}
