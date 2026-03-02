using ERMS.Application.Features.Applications.Queries.GetMyApplications;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Application;
using ERMS.Domain.Entities.Candidate;
using ERMS.Domain.Entities.Enterprise;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Recruitment;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;

// Alias to avoid namespace collision
using ApplicationEntity = ERMS.Domain.Entities.Application.Application;
using CandidateEntity = ERMS.Domain.Entities.Candidate.Candidate;

namespace ERMS.UnitTests.Features.Applications.Queries.GetMyApplications;

public class GetMyApplicationsHandlerTests
{
    private readonly Mock<IERMSDbContext> _contextMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<ILogger<GetMyApplicationsHandler>> _loggerMock;
    private readonly GetMyApplicationsHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _candidateId = Guid.NewGuid();
    private readonly Guid _enterpriseId = Guid.NewGuid();
    private readonly Guid _jobPostingId = Guid.NewGuid();

    public GetMyApplicationsHandlerTests()
    {
        _contextMock = new Mock<IERMSDbContext>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _loggerMock = new Mock<ILogger<GetMyApplicationsHandler>>();

        _handler = new GetMyApplicationsHandler(
            _contextMock.Object,
            _currentUserServiceMock.Object,
            _loggerMock.Object);
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

    private void SetupAuthenticatedCandidate()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(x => x.Roles).Returns([AppRoles.Candidate]);
    }

    private CandidateEntity CreateCandidateProfile()
    {
        return new CandidateEntity
        {
            Id = _candidateId,
            UserId = _userId,
            IsDeleted = false,
            User = new User
            {
                Id = _userId,
                FullName = "Test Candidate",
                Email = "candidate@test.com"
            }
        };
    }

    private Enterprise CreateEnterprise()
    {
        return new Enterprise
        {
            Id = _enterpriseId,
            EnterpriseName = "Test Company",
            EnterpriseCode = "TC001",
            IsDeleted = false
        };
    }

    private JobPosting CreateJobPosting(string jobTitle = "Software Engineer", string? location = "Hanoi")
    {
        return new JobPosting
        {
            Id = _jobPostingId,
            EnterpriseId = _enterpriseId,
            Enterprise = CreateEnterprise(),
            JobTitle = jobTitle,
            JobCode = "SE-001",
            Location = location,
            EmploymentType = "FullTime",
            Description = "Job Description",
            Status = "Published",
            IsDeleted = false
        };
    }

    private ApplicationEntity CreateApplication(
        Guid? applicationId = null,
        string stage = "Applied",
        string status = "Active",
        DateTime? appliedAt = null,
        bool hasInterview = false,
        bool hasOffer = false,
        JobPosting? jobPosting = null)
    {
        var appId = applicationId ?? Guid.NewGuid();
        var jp = jobPosting ?? CreateJobPosting();

        var app = new ApplicationEntity
        {
            Id = appId,
            JobPostingId = jp.Id,
            JobPosting = jp,
            CandidateId = _candidateId,
            Stage = stage,
            Status = status,
            AppliedAt = appliedAt ?? DateTime.UtcNow,
            IsDeleted = false,
            Interviews = hasInterview ? new List<Interview>
            {
                new Interview
                {
                    Id = Guid.NewGuid(),
                    ApplicationId = appId,
                    InterviewType = "Technical",
                    ScheduledAt = DateTime.UtcNow.AddDays(7),
                    ScheduledById = Guid.NewGuid(),
                    Status = "Scheduled"
                }
            } : new List<Interview>(),
            Offer = hasOffer ? new Offer
            {
                Id = Guid.NewGuid(),
                ApplicationId = appId,
                Position = "Software Engineer",
                DepartmentId = 1,
                Salary = 2000,
                StartDate = DateTime.UtcNow.AddMonths(1),
                ExpirationDate = DateTime.UtcNow.AddMonths(2),
                CreatedById = Guid.NewGuid(),
                Status = "Sent"
            } : null
        };

        return app;
    }

    private void SetupCandidatesDbSet(CandidateEntity? candidate = null)
    {
        var data = candidate != null
            ? new List<CandidateEntity> { candidate }.AsQueryable()
            : new List<CandidateEntity>().AsQueryable();

        var mockSet = CreateMockDbSet(data);
        _contextMock.Setup(c => c.Candidates).Returns(mockSet.Object);
    }

    private void SetupApplicationsDbSet(List<ApplicationEntity>? applications = null)
    {
        var data = applications != null
            ? applications.AsQueryable()
            : new List<ApplicationEntity>().AsQueryable();

        var mockSet = CreateMockDbSet(data);
        _contextMock.Setup(c => c.Applications).Returns(mockSet.Object);
    }

    private GetMyApplicationsQuery CreateValidQuery(int pageNumber = 1, int pageSize = 20, string? stageFilter = null)
    {
        return new GetMyApplicationsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            StageFilter = stageFilter
        };
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
            .WithMessage("User not authenticated.");
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserIsNotCandidate()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(x => x.Roles).Returns([AppRoles.HRManager]);

        var query = CreateValidQuery();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(query, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Only candidates can view their applications.");
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserHasNoRoles()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(x => x.Roles).Returns((List<string>?)null);

        var query = CreateValidQuery();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(query, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Only candidates can view their applications.");
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenCandidateProfileNotFound()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(null);

        var query = CreateValidQuery();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(query, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("Candidate profile not found.");
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenCandidateProfileIsDeleted()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        var deletedCandidate = CreateCandidateProfile();
        deletedCandidate.IsDeleted = true;
        SetupCandidatesDbSet(deletedCandidate);

        var query = CreateValidQuery();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(query, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("Candidate profile not found.");
    }

    #endregion

    #region Positive Scenarios - Happy Paths

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenNoApplicationsExist()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateProfile());
        SetupApplicationsDbSet([]);

        var query = CreateValidQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Handle_ShouldReturnApplications_WhenCandidateHasApplications()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateProfile());

        var applications = new List<ApplicationEntity>
        {
            CreateApplication(stage: ApplicationStage.Applied),
            CreateApplication(stage: ApplicationStage.Shortlisted)
        };
        SetupApplicationsDbSet(applications);

        var query = CreateValidQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ShouldPopulateJobDetails_Correctly()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateProfile());

        var jp = CreateJobPosting("Senior Developer", "HCMC");
        var applications = new List<ApplicationEntity>
        {
            CreateApplication(stage: ApplicationStage.Applied, jobPosting: jp)
        };
        SetupApplicationsDbSet(applications);

        var query = CreateValidQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        var item = result.Items[0];
        item.JobTitle.Should().Be("Senior Developer");
        item.Location.Should().Be("HCMC");
        item.CompanyName.Should().Be("Test Company");
        item.EmploymentType.Should().Be("FullTime");
        item.JobCode.Should().Be("SE-001");
    }

    [Fact]
    public async Task Handle_ShouldReturnHasInterviewTrue_WhenApplicationHasInterview()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateProfile());

        var applications = new List<ApplicationEntity>
        {
            CreateApplication(stage: ApplicationStage.InterviewScheduled, hasInterview: true)
        };
        SetupApplicationsDbSet(applications);

        var query = CreateValidQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].HasInterview.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldReturnHasOfferTrue_WhenApplicationHasOffer()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateProfile());

        var applications = new List<ApplicationEntity>
        {
            CreateApplication(stage: ApplicationStage.Offered, hasOffer: true)
        };
        SetupApplicationsDbSet(applications);

        var query = CreateValidQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].HasOffer.Should().BeTrue();
    }

    #endregion

    #region Filtering Tests

    [Fact]
    public async Task Handle_ShouldFilterByStage_WhenStageFilterProvided()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateProfile());

        var applications = new List<ApplicationEntity>
        {
            CreateApplication(stage: ApplicationStage.Applied),
            CreateApplication(stage: ApplicationStage.Shortlisted),
            CreateApplication(stage: ApplicationStage.Applied)
        };
        SetupApplicationsDbSet(applications);

        var query = CreateValidQuery(stageFilter: ApplicationStage.Applied);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(x => x.Stage == ApplicationStage.Applied);
    }

    [Fact]
    public async Task Handle_ShouldReturnAll_WhenStageFilterIsNull()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateProfile());

        var applications = new List<ApplicationEntity>
        {
            CreateApplication(stage: ApplicationStage.Applied),
            CreateApplication(stage: ApplicationStage.Shortlisted)
        };
        SetupApplicationsDbSet(applications);

        var query = CreateValidQuery(stageFilter: null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ShouldExcludeDeletedApplications()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateProfile());

        var activeApp = CreateApplication(stage: ApplicationStage.Applied);
        var deletedApp = CreateApplication(stage: ApplicationStage.Applied);
        deletedApp.IsDeleted = true;

        SetupApplicationsDbSet([activeApp, deletedApp]);

        var query = CreateValidQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_ShouldOnlyReturnOwnApplications()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateProfile());

        var ownApp = CreateApplication(stage: ApplicationStage.Applied);
        var otherApp = CreateApplication(stage: ApplicationStage.Applied);
        otherApp.CandidateId = Guid.NewGuid(); // Different candidate

        SetupApplicationsDbSet([ownApp, otherApp]);

        var query = CreateValidQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
    }

    #endregion

    #region Sorting & Pagination Tests

    [Fact]
    public async Task Handle_ShouldSortByAppliedAtDescending()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateProfile());

        var olderDate = DateTime.UtcNow.AddDays(-5);
        var newerDate = DateTime.UtcNow.AddDays(-1);

        var applications = new List<ApplicationEntity>
        {
            CreateApplication(appliedAt: olderDate),
            CreateApplication(appliedAt: newerDate)
        };
        SetupApplicationsDbSet(applications);

        var query = CreateValidQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items[0].AppliedAt.Should().Be(newerDate);
        result.Items[1].AppliedAt.Should().Be(olderDate);
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectPaginationInfo()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateProfile());

        var applications = new List<ApplicationEntity>();
        for (int i = 0; i < 15; i++)
        {
            applications.Add(CreateApplication(appliedAt: DateTime.UtcNow.AddHours(-i)));
        }
        SetupApplicationsDbSet(applications);

        var query = CreateValidQuery(pageNumber: 2, pageSize: 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(15);
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.Items.Should().HaveCount(5); // 15 total, page 2 of 10 = 5
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
