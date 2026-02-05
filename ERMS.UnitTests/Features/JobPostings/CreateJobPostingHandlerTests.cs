using ERMS.Application.Features.JobPostings.Commands.CreateJobPosting;
using ERMS.Application.Interface;
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

namespace ERMS.UnitTests.Features.JobPostings;

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
    public async Task Should_Throw_UnauthorizedAccessException_When_User_Not_Authenticated()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        var command = CreateValidCommand();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(command, CancellationToken.None));

        Assert.Equal("User not authenticated.", exception.Message);
    }

    [Fact]
    public async Task Should_Throw_UnauthorizedAccessException_When_User_Is_Not_HRManager()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(x => x.Roles).Returns([AppRoles.DepartmentHead]); // Wrong role
        _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(_enterpriseId);

        var command = CreateValidCommand();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(command, CancellationToken.None));

        Assert.Equal("Only HR Manager can create job postings.", exception.Message);
    }

    [Fact]
    public async Task Should_Throw_UnauthorizedAccessException_When_User_Has_No_Roles()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(x => x.Roles).Returns((List<string>?)null);
        _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(_enterpriseId);

        var command = CreateValidCommand();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(command, CancellationToken.None));

        Assert.Equal("Only HR Manager can create job postings.", exception.Message);
    }

    [Fact]
    public async Task Should_Throw_UnauthorizedAccessException_When_User_Has_No_Enterprise()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(x => x.Roles).Returns([AppRoles.HRManager]);
        _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync((Guid?)null);

        var command = CreateValidCommand();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(command, CancellationToken.None));

        Assert.Equal("User is not associated with any enterprise.", exception.Message);
    }

    #endregion

    #region PlanDetail Validation Tests

    [Fact]
    public async Task Should_Throw_Exception_When_PlanDetail_Not_Found()
    {
        // Arrange
        SetupCurrentUser();

        var planDetailsData = new List<PlanDetail>().AsQueryable(); // Empty - no matching PlanDetail
        var planDetailsMockSet = CreateMockDbSet(planDetailsData);
        _contextMock.Setup(c => c.PlanDetails).Returns(planDetailsMockSet.Object);

        var command = CreateValidCommand();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _handler.Handle(command, CancellationToken.None));

        Assert.Contains("PlanDetail with ID", exception.Message);
        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task Should_Throw_BusinessRuleException_When_RecruitmentPlan_Not_Approved()
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
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _handler.Handle(command, CancellationToken.None));

        Assert.Contains("RecruitmentPlan must be 'Approved' by Director", exception.Message);
        Assert.Contains("Pending", exception.Message);
    }

    [Fact]
    public async Task Should_Throw_BusinessRuleException_When_PlanDetail_Status_Is_Not_Approved()
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
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _handler.Handle(command, CancellationToken.None));

        Assert.Contains("PlanDetail status must be 'Approved'", exception.Message);
    }

    [Fact]
    public async Task Should_Throw_BusinessRuleException_When_RequiredSkills_Is_Empty()
    {
        // Arrange
        SetupCurrentUser();

        var planDetail = CreateApprovedPlanDetail(quantity: 3, requiredSkills: ""); // Empty skills
        SetupFullMocks(planDetail);

        var command = CreateValidCommand();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _handler.Handle(command, CancellationToken.None));

        Assert.Contains("RequiredSkills is empty", exception.Message);
        Assert.Contains("AI CV scanning", exception.Message);
    }

    [Fact]
    public async Task Should_Throw_BusinessRuleException_When_RequiredSkills_Is_Whitespace()
    {
        // Arrange
        SetupCurrentUser();

        var planDetail = CreateApprovedPlanDetail(quantity: 3, requiredSkills: "   "); // Whitespace
        SetupFullMocks(planDetail);

        var command = CreateValidCommand();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _handler.Handle(command, CancellationToken.None));

        Assert.Contains("RequiredSkills is empty", exception.Message);
    }

    #endregion

    #region Quota Validation Tests

    [Fact]
    public async Task Should_Throw_BusinessRuleException_When_Plan_Quota_Exhausted()
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
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _handler.Handle(command, CancellationToken.None));

        Assert.Contains("Quota exhausted", exception.Message);
        Assert.Contains("Required: 2", exception.Message);
        Assert.Contains("Hired: 2", exception.Message);
    }

    [Fact]
    public async Task Should_Succeed_When_Quota_Has_Remaining_Slots()
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
        Assert.NotEqual(Guid.Empty, result);
    }

    #endregion

    #region Auto-Fill Logic Tests

    [Fact]
    public async Task Should_Copy_RequiredSkills_To_Requirements_For_AI_Scanning()
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
        Assert.NotNull(capturedJobPosting);
        Assert.Equal(expectedRequirements, capturedJobPosting.Requirements);
        Assert.Equal(planDetail.PositionTitle, capturedJobPosting.JobTitle);
        Assert.Equal(JobPostingStatus.Draft, capturedJobPosting.Status);
    }

    [Fact]
    public async Task Should_Use_TitleOverride_When_Provided()
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
        Assert.NotNull(capturedJobPosting);
        Assert.Equal("Senior .NET Developer", capturedJobPosting.JobTitle); // Trimmed
    }

    [Fact]
    public async Task Should_Use_PositionTitle_When_TitleOverride_Is_Empty()
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
        Assert.NotNull(capturedJobPosting);
        Assert.Equal("Software Engineer", capturedJobPosting.JobTitle); // From PlanDetail
    }

    [Fact]
    public async Task Should_Use_DescriptionOverride_When_Provided()
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
        Assert.NotNull(capturedJobPosting);
        Assert.Equal("Custom job description here", capturedJobPosting.Description); // Trimmed
    }

    [Fact]
    public async Task Should_Generate_Default_Description_When_DescriptionOverride_Is_Empty()
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
        Assert.NotNull(capturedJobPosting);
        Assert.Equal("We are looking for a Software Engineer.", capturedJobPosting.Description);
    }

    [Fact]
    public async Task Should_Set_Default_Values_Correctly()
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
        Assert.NotNull(capturedJobPosting);
        Assert.Equal(JobPostingStatus.Draft, capturedJobPosting.Status);
        Assert.Equal("Full-time", capturedJobPosting.EmploymentType);
        Assert.True(capturedJobPosting.ShowSalary);
        Assert.Equal(0, capturedJobPosting.ViewCount);
        Assert.Equal(0, capturedJobPosting.ApplicationCount);
        Assert.False(capturedJobPosting.IsDeleted);
        Assert.Equal(_userId, capturedJobPosting.CreatedById);
        Assert.Equal(_enterpriseId, capturedJobPosting.EnterpriseId);
        Assert.Equal(_planDetailId, capturedJobPosting.PlanDetailId);
        Assert.Equal(1, capturedJobPosting.DepartmentId); // From RecruitmentPlan
    }

    [Fact]
    public async Task Should_Copy_Salary_Range_From_PlanDetail()
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
        Assert.NotNull(capturedJobPosting);
        Assert.Equal(15000000, capturedJobPosting.SalaryRangeMin);
        Assert.Equal(30000000, capturedJobPosting.SalaryRangeMax);
        Assert.Equal("2-5 years", capturedJobPosting.ExperienceLevel);
        Assert.Equal(3, capturedJobPosting.Quantity);
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

// Async query provider for EF Core mocking
internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    public TestAsyncQueryProvider(IQueryProvider inner) => _inner = inner;

    public IQueryable CreateQuery(System.Linq.Expressions.Expression expression)
        => new TestAsyncEnumerable<TEntity>(expression);

    public IQueryable<TElement> CreateQuery<TElement>(System.Linq.Expressions.Expression expression)
        => new TestAsyncEnumerable<TElement>(expression);

    public object? Execute(System.Linq.Expressions.Expression expression)
        => _inner.Execute(expression);

    public TResult Execute<TResult>(System.Linq.Expressions.Expression expression)
        => _inner.Execute<TResult>(expression);

    public TResult ExecuteAsync<TResult>(System.Linq.Expressions.Expression expression, CancellationToken cancellationToken = default)
    {
        var resultType = typeof(TResult).GetGenericArguments()[0];
        var executionResult = typeof(IQueryProvider)
            .GetMethod(nameof(IQueryProvider.Execute), 1, [typeof(System.Linq.Expressions.Expression)])!
            .MakeGenericMethod(resultType)
            .Invoke(_inner, [expression]);

        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(resultType)
            .Invoke(null, [executionResult])!;
    }
}

internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
    public TestAsyncEnumerable(System.Linq.Expressions.Expression expression) : base(expression) { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;

    public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;

    public T Current => _inner.Current;

    public ValueTask<bool> MoveNextAsync() => new(_inner.MoveNext());

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return ValueTask.CompletedTask;
    }
}
