using ERMS.Application.Features.Applications.Commands.ForwardApplication;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Recruitment;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;

// Alias to avoid namespace collision
using ApplicationEntity = ERMS.Domain.Entities.Application.Application;

namespace ERMS.UnitTests.Features.Applications.Commands.ForwardApplication;

public class ForwardApplicationHandlerTests
{
    private readonly Mock<IERMSDbContext> _contextMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<ILogger<ForwardApplicationHandler>> _loggerMock;
    private readonly ForwardApplicationHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _enterpriseId = Guid.NewGuid();
    private readonly Guid _applicationId = Guid.NewGuid();
    private readonly Guid _jobPostingId = Guid.NewGuid();
    private readonly Guid _otherEnterpriseId = Guid.NewGuid();

    public ForwardApplicationHandlerTests()
    {
        _contextMock = new Mock<IERMSDbContext>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _loggerMock = new Mock<ILogger<ForwardApplicationHandler>>();

        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new ForwardApplicationHandler(
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

    private void SetupAuthenticatedHRManager()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(x => x.Roles).Returns([AppRoles.HRManager]);
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

    private ApplicationEntity CreateApplication(string stage = "Applied", Guid? enterpriseId = null)
    {
        return new ApplicationEntity
        {
            Id = _applicationId,
            JobPostingId = _jobPostingId,
            JobPosting = CreateJobPosting(enterpriseId),
            CandidateId = Guid.NewGuid(),
            Stage = stage,
            Status = "Active",
            AppliedAt = DateTime.UtcNow.AddDays(-1),
            IsDeleted = false
        };
    }

    private ForwardApplicationCommand CreateValidCommand()
    {
        return new ForwardApplicationCommand
        {
            ApplicationId = _applicationId,
            HRNote = null
        };
    }

    private void SetupApplicationsDbSet(ApplicationEntity? application)
    {
        var data = application != null
            ? new List<ApplicationEntity> { application }.AsQueryable()
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

        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("User not authenticated.");
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserIsNotHRManager()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(x => x.Roles).Returns([AppRoles.Director]); // Not HRManager

        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Only HR Manager can forward applications.");
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserIsCandidate()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(x => x.Roles).Returns([AppRoles.Candidate]);

        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Only HR Manager can forward applications.");
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserIsDepartmentHead()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(x => x.Roles).Returns([AppRoles.DepartmentHead]);

        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Only HR Manager can forward applications.");
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserHasNoRoles()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(x => x.Roles).Returns((List<string>?)null);

        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Only HR Manager can forward applications.");
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserNotAssociatedWithEnterprise()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(x => x.Roles).Returns([AppRoles.HRManager]);
        _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync((Guid?)null);

        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("User is not associated with any enterprise.");
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenApplicationBelongsToDifferentEnterprise()
    {
        // Arrange - Data Isolation Test
        SetupAuthenticatedHRManager();

        var application = CreateApplication(ApplicationStage.Applied, _otherEnterpriseId);
        SetupApplicationsDbSet(application);

        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("You do not have permission to access this application.");
    }

    #endregion

    #region Application Not Found Tests

    [Fact]
    public async Task Handle_ShouldThrowException_WhenApplicationNotFound()
    {
        // Arrange
        SetupAuthenticatedHRManager();
        SetupApplicationsDbSet(null); // No application

        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage($"*Application with ID {_applicationId} not found*");
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenApplicationIsDeleted()
    {
        // Arrange
        SetupAuthenticatedHRManager();

        var application = CreateApplication();
        application.IsDeleted = true;
        SetupApplicationsDbSet(application);

        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage($"*Application with ID {_applicationId} not found*");
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenApplicationIdIsEmpty()
    {
        // Arrange
        SetupAuthenticatedHRManager();
        SetupApplicationsDbSet(null);

        var command = new ForwardApplicationCommand
        {
            ApplicationId = Guid.Empty
        };

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("*not found*");
    }

    #endregion

    #region Business Rule Violation Tests - Stage Validation

    [Fact]
    public async Task Handle_ShouldThrowException_WhenApplicationStageIsNotApplied()
    {
        // Arrange
        SetupAuthenticatedHRManager();

        var application = CreateApplication(ApplicationStage.Shortlisted); // Wrong stage
        SetupApplicationsDbSet(application);

        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage($"*Cannot forward application*current stage is 'Shortlisted'*expected 'Applied'*");
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenApplicationIsAlreadyRejected()
    {
        // Arrange
        SetupAuthenticatedHRManager();

        var application = CreateApplication(ApplicationStage.Rejected);
        SetupApplicationsDbSet(application);

        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("*Cannot forward application*current stage is 'Rejected'*");
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenApplicationIsAlreadyHired()
    {
        // Arrange
        SetupAuthenticatedHRManager();

        var application = CreateApplication(ApplicationStage.Hired);
        SetupApplicationsDbSet(application);

        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("*Cannot forward application*current stage is 'Hired'*");
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenApplicationIsInterviewed()
    {
        // Arrange
        SetupAuthenticatedHRManager();

        var application = CreateApplication(ApplicationStage.Interviewed);
        SetupApplicationsDbSet(application);

        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("*Cannot forward application*current stage is 'Interviewed'*");
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenApplicationIsWithdrawn()
    {
        // Arrange
        SetupAuthenticatedHRManager();

        var application = CreateApplication(ApplicationStage.Withdrawn);
        SetupApplicationsDbSet(application);

        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("*Cannot forward application*current stage is 'Withdrawn'*");
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenApplicationIsOffered()
    {
        // Arrange
        SetupAuthenticatedHRManager();

        var application = CreateApplication(ApplicationStage.Offered);
        SetupApplicationsDbSet(application);

        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("*Cannot forward application*current stage is 'Offered'*");
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenApplicationIsInterviewScheduled()
    {
        // Arrange
        SetupAuthenticatedHRManager();

        var application = CreateApplication(ApplicationStage.InterviewScheduled);
        SetupApplicationsDbSet(application);

        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("*Cannot forward application*current stage is 'InterviewScheduled'*");
    }

    #endregion

    #region Positive Scenarios - Happy Paths

    [Fact]
    public async Task Handle_ShouldSuccessfullyForwardApplication_WhenStageIsApplied()
    {
        // Arrange
        SetupAuthenticatedHRManager();

        var application = CreateApplication(ApplicationStage.Applied);
        SetupApplicationsDbSet(application);

        var command = CreateValidCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ApplicationId.Should().Be(_applicationId);
        result.PreviousStage.Should().Be(ApplicationStage.Applied);
        result.NewStage.Should().Be(ApplicationStage.Shortlisted);
        result.StageUpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_ShouldUpdateApplicationStageToShortlisted()
    {
        // Arrange
        SetupAuthenticatedHRManager();

        var application = CreateApplication(ApplicationStage.Applied);
        SetupApplicationsDbSet(application);

        var command = CreateValidCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        application.Stage.Should().Be(ApplicationStage.Shortlisted);
    }

    [Fact]
    public async Task Handle_ShouldUpdateStageUpdatedAtTimestamp()
    {
        // Arrange
        SetupAuthenticatedHRManager();

        var application = CreateApplication(ApplicationStage.Applied);
        application.StageUpdatedAt = DateTime.UtcNow.AddDays(-10); // Old timestamp
        SetupApplicationsDbSet(application);

        var command = CreateValidCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        application.StageUpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_ShouldUpdateUpdatedAtTimestamp()
    {
        // Arrange
        SetupAuthenticatedHRManager();

        var application = CreateApplication(ApplicationStage.Applied);
        application.UpdatedAt = DateTime.UtcNow.AddDays(-10); // Old timestamp
        SetupApplicationsDbSet(application);

        var command = CreateValidCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        application.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_ShouldSaveChangesToDatabase()
    {
        // Arrange
        SetupAuthenticatedHRManager();

        var application = CreateApplication(ApplicationStage.Applied);
        SetupApplicationsDbSet(application);

        var command = CreateValidCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region HR Note Tests

    [Fact]
    public async Task Handle_ShouldUpdateHRNote_WhenProvided()
    {
        // Arrange
        SetupAuthenticatedHRManager();

        var application = CreateApplication(ApplicationStage.Applied);
        SetupApplicationsDbSet(application);

        var command = new ForwardApplicationCommand
        {
            ApplicationId = _applicationId,
            HRNote = "Strong candidate, recommend for technical interview."
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        application.HRNote.Should().Be("Strong candidate, recommend for technical interview.");
        result.HRNote.Should().Be("Strong candidate, recommend for technical interview.");
    }

    [Fact]
    public async Task Handle_ShouldTrimHRNote_WhenProvidedWithWhitespace()
    {
        // Arrange
        SetupAuthenticatedHRManager();

        var application = CreateApplication(ApplicationStage.Applied);
        SetupApplicationsDbSet(application);

        var command = new ForwardApplicationCommand
        {
            ApplicationId = _applicationId,
            HRNote = "   Good candidate for the role.   "
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        application.HRNote.Should().Be("Good candidate for the role.");
    }

    [Fact]
    public async Task Handle_ShouldNotUpdateHRNote_WhenNull()
    {
        // Arrange
        SetupAuthenticatedHRManager();

        var application = CreateApplication(ApplicationStage.Applied);
        application.HRNote = "Existing note";
        SetupApplicationsDbSet(application);

        var command = new ForwardApplicationCommand
        {
            ApplicationId = _applicationId,
            HRNote = null
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        application.HRNote.Should().Be("Existing note"); // Unchanged
    }

    [Fact]
    public async Task Handle_ShouldNotUpdateHRNote_WhenEmpty()
    {
        // Arrange
        SetupAuthenticatedHRManager();

        var application = CreateApplication(ApplicationStage.Applied);
        application.HRNote = "Existing note";
        SetupApplicationsDbSet(application);

        var command = new ForwardApplicationCommand
        {
            ApplicationId = _applicationId,
            HRNote = ""
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        application.HRNote.Should().Be("Existing note"); // Unchanged
    }

    [Fact]
    public async Task Handle_ShouldNotUpdateHRNote_WhenOnlyWhitespace()
    {
        // Arrange
        SetupAuthenticatedHRManager();

        var application = CreateApplication(ApplicationStage.Applied);
        application.HRNote = "Existing note";
        SetupApplicationsDbSet(application);

        var command = new ForwardApplicationCommand
        {
            ApplicationId = _applicationId,
            HRNote = "   "
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        application.HRNote.Should().Be("Existing note"); // Unchanged
    }

    #endregion

    #region Return Value Tests

    [Fact]
    public async Task Handle_ShouldReturnCorrectPreviousStage()
    {
        // Arrange
        SetupAuthenticatedHRManager();

        var application = CreateApplication(ApplicationStage.Applied);
        SetupApplicationsDbSet(application);

        var command = CreateValidCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.PreviousStage.Should().Be(ApplicationStage.Applied);
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectNewStage()
    {
        // Arrange
        SetupAuthenticatedHRManager();

        var application = CreateApplication(ApplicationStage.Applied);
        SetupApplicationsDbSet(application);

        var command = CreateValidCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.NewStage.Should().Be(ApplicationStage.Shortlisted);
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectApplicationId()
    {
        // Arrange
        SetupAuthenticatedHRManager();

        var application = CreateApplication(ApplicationStage.Applied);
        SetupApplicationsDbSet(application);

        var command = CreateValidCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ApplicationId.Should().Be(_applicationId);
    }

    #endregion

    #region Case Sensitivity Tests

    [Fact]
    public async Task Handle_ShouldAcceptAppliedStage_CaseInsensitive_Lowercase()
    {
        // Arrange
        SetupAuthenticatedHRManager();

        var application = CreateApplication("applied"); // lowercase
        SetupApplicationsDbSet(application);

        var command = CreateValidCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.NewStage.Should().Be(ApplicationStage.Shortlisted);
    }

    [Fact]
    public async Task Handle_ShouldAcceptAppliedStage_CaseInsensitive_Uppercase()
    {
        // Arrange
        SetupAuthenticatedHRManager();

        var application = CreateApplication("APPLIED"); // uppercase
        SetupApplicationsDbSet(application);

        var command = CreateValidCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.NewStage.Should().Be(ApplicationStage.Shortlisted);
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
