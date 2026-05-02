using ERMS.Application.Features.Applications.Queries.GetApplicationsByJob;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Application;
using ERMS.Domain.Entities.Candidate;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Recruitment;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using System.Linq.Expressions;

// Alias to avoid namespace collision
using ApplicationEntity = ERMS.Domain.Entities.Application.Application;
using CandidateEntity = ERMS.Domain.Entities.Candidate.Candidate;

namespace ERMS.UnitTests.Features.Applications.Queries.GetApplicationsByJob;

public class GetApplicationsByJobHandlerTests
{
    private readonly Mock<IERMSDbContext> _contextMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly GetApplicationsByJobHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _enterpriseId = Guid.NewGuid();
    private readonly Guid _jobPostingId = Guid.NewGuid();
    private readonly Guid _otherEnterpriseId = Guid.NewGuid();

    public GetApplicationsByJobHandlerTests()
    {
        _contextMock = new Mock<IERMSDbContext>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();

        _handler = new GetApplicationsByJobHandler(
            _contextMock.Object,
            _currentUserServiceMock.Object);
    }

    #region Helper Methods

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

    private void SetupAuthenticatedHRManager()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(x => x.Roles).Returns([AppRoles.HRManager]);
        _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(_enterpriseId);
    }

    private void SetupAuthenticatedDirector()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(x => x.Roles).Returns([AppRoles.Director]);
        _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(_enterpriseId);
    }

    private JobPosting CreateJobPosting(Guid? enterpriseId = null)
    {
        return new JobPosting
        {
            Id = _jobPostingId,
            EnterpriseId = enterpriseId ?? _enterpriseId,
            JobTitle = "Software Engineer",
            Description = "Looking for a .NET developer",
            Status = "Published",
            IsDeleted = false
        };
    }

    private ApplicationEntity CreateApplication(
        Guid applicationId,
        Guid candidateId,
        decimal overallScore,
        DateTime appliedAt,
        string stage = "Applied")
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = $"Candidate {candidateId}",
            Email = $"candidate{candidateId}@test.com",
            PhoneNumber = "0123456789"
        };

        var candidate = new CandidateEntity
        {
            Id = candidateId,
            UserId = user.Id,
            User = user,
            IsDeleted = false
        };

        var resume = new Resume
        {
            Id = Guid.NewGuid(),
            CandidateId = candidateId,
            FileName = "resume.pdf",
            FileUrl = "https://cloudinary.com/resume.pdf"
        };

        var cvScreeningResult = new CVScreeningResult
        {
            Id = Guid.NewGuid(),
            ApplicationId = applicationId,
            OverallScore = overallScore,
            SkillMatchScore = 80,
            ExperienceMatchScore = 75,
            Summary = $"Score: {overallScore}"
        };

        return new ApplicationEntity
        {
            Id = applicationId,
            JobPostingId = _jobPostingId,
            CandidateId = candidateId,
            Candidate = candidate,
            Resume = resume,
            ResumeId = resume.Id,
            CVScreeningResult = cvScreeningResult,
            Stage = stage,
            Status = "Active",
            AppliedAt = appliedAt,
            IsDeleted = false
        };
    }

    private GetApplicationsByJobQuery CreateValidQuery()
    {
        return new GetApplicationsByJobQuery
        {
            JobPostingId = _jobPostingId,
            PageNumber = 1,
            PageSize = 20
        };
    }

    private void SetupJobPostingDbSet(JobPosting? jobPosting = null)
    {
        var data = jobPosting != null
            ? new List<JobPosting> { jobPosting }.AsQueryable()
            : new List<JobPosting>().AsQueryable();

        var mockSet = CreateMockDbSet(data);
        _contextMock.Setup(c => c.JobPostings).Returns(mockSet.Object);
    }

    private void SetupApplicationsDbSet(List<ApplicationEntity>? applications = null)
    {
        var data = applications != null
            ? applications.AsQueryable()
            : new List<ApplicationEntity>().AsQueryable();

        var mockSet = CreateMockDbSet(data);
        _contextMock.Setup(c => c.Applications).Returns(mockSet.Object);
    }

    #endregion

    #region Security & Authorization Tests

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserNotAuthenticated()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        var query = CreateValidQuery();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(query, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Người dùng chưa được xác thực.");
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserIsNotHRManagerOrDirector()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(x => x.Roles).Returns([AppRoles.Candidate]); // Wrong role
        _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(_enterpriseId);

        var query = CreateValidQuery();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(query, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Chỉ HR Manager hoặc Giám đốc mới có quyền xem hồ sơ ứng tuyển.");
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserHasNoRoles()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(x => x.Roles).Returns((List<string>?)null);
        _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(_enterpriseId);

        var query = CreateValidQuery();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(query, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Chỉ HR Manager hoặc Giám đốc mới có quyền xem hồ sơ ứng tuyển.");
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserHasEmployeeRole()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(x => x.Roles).Returns([AppRoles.Employee]);
        _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(_enterpriseId);

        var query = CreateValidQuery();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(query, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Chỉ HR Manager hoặc Giám đốc mới có quyền xem hồ sơ ứng tuyển.");
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserNotAssociatedWithEnterprise()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(x => x.Roles).Returns([AppRoles.HRManager]);
        _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync((Guid?)null);

        var query = CreateValidQuery();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(query, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Người dùng không thuộc doanh nghiệp nào.");
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenJobPostingBelongsToDifferentEnterprise()
    {
        // Arrange - Data Isolation Test
        SetupAuthenticatedHRManager();

        var jobPosting = CreateJobPosting(_otherEnterpriseId); // Different enterprise
        SetupJobPostingDbSet(jobPosting);

        var query = CreateValidQuery();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(query, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage($"*Tin tuyển dụng với ID {_jobPostingId}*");
    }

    #endregion

    #region Job Posting Validation Tests

    [Fact]
    public async Task Handle_ShouldThrowException_WhenJobPostingNotFound()
    {
        // Arrange
        SetupAuthenticatedHRManager();
        SetupJobPostingDbSet(null); // No job posting

        var query = CreateValidQuery();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(query, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage($"*Tin tuyển dụng với ID {_jobPostingId}*");
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenJobPostingIsDeleted()
    {
        // Arrange
        SetupAuthenticatedHRManager();

        var jobPosting = CreateJobPosting();
        jobPosting.IsDeleted = true;
        SetupJobPostingDbSet(jobPosting);

        var query = CreateValidQuery();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(query, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage($"*Tin tuyển dụng với ID {_jobPostingId}*");
    }

    #endregion

    #region Positive Scenarios - Happy Paths

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenNoApplicationsExist()
    {
        // Arrange
        SetupAuthenticatedHRManager();
        SetupJobPostingDbSet(CreateJobPosting());
        SetupApplicationsDbSet([]); // Empty

        var query = CreateValidQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.JobPostingId.Should().Be(_jobPostingId);
        result.JobTitle.Should().Be("Software Engineer");
    }

    [Fact]
    public async Task Handle_ShouldReturnApplications_WhenHRManagerIsAuthenticated()
    {
        // Arrange
        SetupAuthenticatedHRManager();
        SetupJobPostingDbSet(CreateJobPosting());

        var applications = new List<ApplicationEntity>
        {
            CreateApplication(Guid.NewGuid(), Guid.NewGuid(), 85, DateTime.UtcNow.AddHours(-1))
        };
        SetupApplicationsDbSet(applications);

        var query = CreateValidQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldReturnApplications_WhenDirectorIsAuthenticated()
    {
        // Arrange
        SetupAuthenticatedDirector();
        SetupJobPostingDbSet(CreateJobPosting());

        var applications = new List<ApplicationEntity>
        {
            CreateApplication(Guid.NewGuid(), Guid.NewGuid(), 90, DateTime.UtcNow)
        };
        SetupApplicationsDbSet(applications);

        var query = CreateValidQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectJobInfo_InResponse()
    {
        // Arrange
        SetupAuthenticatedHRManager();
        SetupJobPostingDbSet(CreateJobPosting());
        SetupApplicationsDbSet([]);

        var query = CreateValidQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.JobPostingId.Should().Be(_jobPostingId);
        result.JobTitle.Should().Be("Software Engineer");
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    #endregion

    #region Sorting Logic Verification Tests

    [Fact]
    public async Task Handle_ShouldSortByOverallScoreDescending_HighestScoreFirst()
    {
        // Arrange
        SetupAuthenticatedHRManager();
        SetupJobPostingDbSet(CreateJobPosting());

        var applications = new List<ApplicationEntity>
        {
            CreateApplication(Guid.NewGuid(), Guid.NewGuid(), 70, DateTime.UtcNow.AddHours(-3)), // 3rd
            CreateApplication(Guid.NewGuid(), Guid.NewGuid(), 95, DateTime.UtcNow.AddHours(-1)), // 1st (highest score)
            CreateApplication(Guid.NewGuid(), Guid.NewGuid(), 80, DateTime.UtcNow.AddHours(-2))  // 2nd
        };
        SetupApplicationsDbSet(applications);

        var query = CreateValidQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert - Verify descending order by score
        result.Items.Should().HaveCount(3);
        result.Items[0].OverallScore.Should().Be(95);
        result.Items[1].OverallScore.Should().Be(80);
        result.Items[2].OverallScore.Should().Be(70);
    }

    [Fact]
    public async Task Handle_ShouldSortByAppliedAtDescending_WhenScoresAreEqual()
    {
        // Arrange
        SetupAuthenticatedHRManager();
        SetupJobPostingDbSet(CreateJobPosting());

        var newerAppliedAt = DateTime.UtcNow.AddHours(-1);
        var olderAppliedAt = DateTime.UtcNow.AddHours(-5);

        var applications = new List<ApplicationEntity>
        {
            CreateApplication(Guid.NewGuid(), Guid.NewGuid(), 85, olderAppliedAt),  // 2nd (same score, older)
            CreateApplication(Guid.NewGuid(), Guid.NewGuid(), 85, newerAppliedAt)   // 1st (same score, newer)
        };
        SetupApplicationsDbSet(applications);

        var query = CreateValidQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert - When scores are equal, newer applications come first
        result.Items.Should().HaveCount(2);
        result.Items[0].AppliedAt.Should().Be(newerAppliedAt);
        result.Items[1].AppliedAt.Should().Be(olderAppliedAt);
    }

    [Fact]
    public async Task Handle_ShouldSortCorrectly_WithMixedScoresAndDates()
    {
        // Arrange
        SetupAuthenticatedHRManager();
        SetupJobPostingDbSet(CreateJobPosting());

        var time1 = DateTime.UtcNow.AddHours(-1);
        var time2 = DateTime.UtcNow.AddHours(-2);
        var time3 = DateTime.UtcNow.AddHours(-3);
        var time4 = DateTime.UtcNow.AddHours(-4);

        var applications = new List<ApplicationEntity>
        {
            CreateApplication(Guid.NewGuid(), Guid.NewGuid(), 70, time1),  // 4th (lowest score)
            CreateApplication(Guid.NewGuid(), Guid.NewGuid(), 90, time3),  // 2nd (high score, older)
            CreateApplication(Guid.NewGuid(), Guid.NewGuid(), 90, time2),  // 1st (high score, newer)
            CreateApplication(Guid.NewGuid(), Guid.NewGuid(), 85, time4)   // 3rd (middle score)
        };
        SetupApplicationsDbSet(applications);

        var query = CreateValidQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(4);
        result.Items[0].OverallScore.Should().Be(90);
        result.Items[0].AppliedAt.Should().Be(time2); // Newer of the 90-score apps
        result.Items[1].OverallScore.Should().Be(90);
        result.Items[1].AppliedAt.Should().Be(time3); // Older of the 90-score apps
        result.Items[2].OverallScore.Should().Be(85);
        result.Items[3].OverallScore.Should().Be(70);
    }

    #endregion

    #region Filtering Tests

    [Fact]
    public async Task Handle_ShouldFilterByStage_WhenStageFilterProvided()
    {
        // Arrange
        SetupAuthenticatedHRManager();
        SetupJobPostingDbSet(CreateJobPosting());

        var applications = new List<ApplicationEntity>
        {
            CreateApplication(Guid.NewGuid(), Guid.NewGuid(), 90, DateTime.UtcNow, ApplicationStage.Applied),
            CreateApplication(Guid.NewGuid(), Guid.NewGuid(), 85, DateTime.UtcNow, ApplicationStage.Shortlisted),
            CreateApplication(Guid.NewGuid(), Guid.NewGuid(), 80, DateTime.UtcNow, ApplicationStage.Applied)
        };
        SetupApplicationsDbSet(applications);

        var query = new GetApplicationsByJobQuery
        {
            JobPostingId = _jobPostingId,
            PageNumber = 1,
            PageSize = 20,
            StageFilter = ApplicationStage.Applied
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(x => x.Stage == ApplicationStage.Applied);
    }

    [Fact]
    public async Task Handle_ShouldReturnAll_WhenStageFilterIsNullOrEmpty()
    {
        // Arrange
        SetupAuthenticatedHRManager();
        SetupJobPostingDbSet(CreateJobPosting());

        var applications = new List<ApplicationEntity>
        {
            CreateApplication(Guid.NewGuid(), Guid.NewGuid(), 90, DateTime.UtcNow, ApplicationStage.Applied),
            CreateApplication(Guid.NewGuid(), Guid.NewGuid(), 85, DateTime.UtcNow, ApplicationStage.Shortlisted)
        };
        SetupApplicationsDbSet(applications);

        var query = new GetApplicationsByJobQuery
        {
            JobPostingId = _jobPostingId,
            PageNumber = 1,
            PageSize = 20,
            StageFilter = null
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ShouldExcludeDeletedApplications()
    {
        // Arrange
        SetupAuthenticatedHRManager();
        SetupJobPostingDbSet(CreateJobPosting());

        var activeApp = CreateApplication(Guid.NewGuid(), Guid.NewGuid(), 90, DateTime.UtcNow);
        var deletedApp = CreateApplication(Guid.NewGuid(), Guid.NewGuid(), 95, DateTime.UtcNow);
        deletedApp.IsDeleted = true;

        SetupApplicationsDbSet([activeApp, deletedApp]);

        var query = CreateValidQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].OverallScore.Should().Be(90);
    }

    #endregion

    #region Boundary Conditions Tests

    [Fact]
    public async Task Handle_ShouldReturnSingleItem_WhenOnlyOneApplicationExists()
    {
        // Arrange
        SetupAuthenticatedHRManager();
        SetupJobPostingDbSet(CreateJobPosting());

        var applications = new List<ApplicationEntity>
        {
            CreateApplication(Guid.NewGuid(), Guid.NewGuid(), 75, DateTime.UtcNow)
        };
        SetupApplicationsDbSet(applications);

        var query = CreateValidQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        result.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldHandleApplicationsWithNullCVScreeningResult()
    {
        // Arrange
        SetupAuthenticatedHRManager();
        SetupJobPostingDbSet(CreateJobPosting());

        var appWithScore = CreateApplication(Guid.NewGuid(), Guid.NewGuid(), 85, DateTime.UtcNow);
        var appWithoutScore = CreateApplication(Guid.NewGuid(), Guid.NewGuid(), 0, DateTime.UtcNow);
        appWithoutScore.CVScreeningResult = null; // No AI screening

        SetupApplicationsDbSet([appWithScore, appWithoutScore]);

        var query = CreateValidQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert - App with score should come first, app without score treated as 0
        result.Items.Should().HaveCount(2);
        result.Items[0].OverallScore.Should().Be(85);
        result.Items[1].OverallScore.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectPaginationInfo()
    {
        // Arrange
        SetupAuthenticatedHRManager();
        SetupJobPostingDbSet(CreateJobPosting());

        var applications = new List<ApplicationEntity>();
        for (int i = 0; i < 25; i++)
        {
            applications.Add(CreateApplication(Guid.NewGuid(), Guid.NewGuid(), 50 + i, DateTime.UtcNow.AddHours(-i)));
        }
        SetupApplicationsDbSet(applications);

        var query = new GetApplicationsByJobQuery
        {
            JobPostingId = _jobPostingId,
            PageNumber = 2,
            PageSize = 10
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(25);
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(3);
        result.Items.Should().HaveCount(10);
    }

    #endregion
}

#region Test Infrastructure

internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    internal TestAsyncQueryProvider(IQueryProvider inner)
    {
        _inner = inner;
    }

    public IQueryable CreateQuery(Expression expression)
    {
        return new TestAsyncEnumerable<TEntity>(expression);
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new TestAsyncEnumerable<TElement>(expression);
    }

    public object Execute(Expression expression)
    {
        return _inner.Execute(expression)!;
    }

    public TResult Execute<TResult>(Expression expression)
    {
        return _inner.Execute<TResult>(expression);
    }

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
    {
        var expectedResultType = typeof(TResult).GetGenericArguments()[0];
        var executionResult = typeof(IQueryProvider)
            .GetMethod(
                name: nameof(IQueryProvider.Execute),
                genericParameterCount: 1,
                types: [typeof(Expression)])!
            .MakeGenericMethod(expectedResultType)
            .Invoke(this, [expression]);

        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(expectedResultType)
            .Invoke(null, [executionResult])!;
    }
}

internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable)
        : base(enumerable)
    { }

    public TestAsyncEnumerable(Expression expression)
        : base(expression)
    { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
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

    public T Current => _inner.Current;
}

#endregion
