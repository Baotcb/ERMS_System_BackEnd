using ERMS.Application.Features.Applications.Commands.WithdrawApplication;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Candidate;
using ERMS.Domain.Entities.Recruitment;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

// Alias to avoid namespace collision
using ApplicationEntity = ERMS.Domain.Entities.Application.Application;

namespace ERMS.UnitTests.Features.Applications.Commands.WithdrawApplication;

public class WithdrawApplicationHandlerTests
{
    private readonly Mock<IERMSDbContext> _contextMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<ILogger<WithdrawApplicationHandler>> _loggerMock;
    private readonly WithdrawApplicationHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _candidateId = Guid.NewGuid();
    private readonly Guid _applicationId = Guid.NewGuid();
    private readonly Guid _jobPostingId = Guid.NewGuid();
    private readonly Guid _otherCandidateId = Guid.NewGuid();

    public WithdrawApplicationHandlerTests()
    {
        _contextMock = new Mock<IERMSDbContext>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _loggerMock = new Mock<ILogger<WithdrawApplicationHandler>>();

        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new WithdrawApplicationHandler(
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

    private Candidate CreateCandidateEntity(Guid? userId = null, Guid? candidateId = null)
    {
        return new Candidate
        {
            Id = candidateId ?? _candidateId,
            UserId = userId ?? _userId,
            IsDeleted = false
        };
    }

    private ApplicationEntity CreateApplication(string stage = "Applied", Guid? candidateId = null)
    {
        return new ApplicationEntity
        {
            Id = _applicationId,
            JobPostingId = _jobPostingId,
            CandidateId = candidateId ?? _candidateId,
            Stage = stage,
            Status = "Active",
            AppliedAt = DateTime.UtcNow.AddDays(-1),
            IsDeleted = false
        };
    }

    private WithdrawApplicationCommand CreateValidCommand()
    {
        return new WithdrawApplicationCommand
        {
            ApplicationId = _applicationId
        };
    }

    private WithdrawApplicationCommand CreateValidCommand(string? reason)
    {
        return new WithdrawApplicationCommand
        {
            ApplicationId = _applicationId,
            Reason = reason
        };
    }

    private void SetupCandidatesDbSet(Candidate? candidate)
    {
        var data = candidate != null
            ? new List<Candidate> { candidate }.AsQueryable()
            : new List<Candidate>().AsQueryable();

        var mockSet = CreateMockDbSet(data);
        _contextMock.Setup(c => c.Candidates).Returns(mockSet.Object);
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
            .WithMessage("Người dùng chưa được xác thực.");
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserIsNotCandidate()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(x => x.Roles).Returns([AppRoles.HRManager]);

        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Chỉ ứng viên mới có quyền rút hồ sơ ứng tuyển.");
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
            .WithMessage("Chỉ ứng viên mới có quyền rút hồ sơ ứng tuyển.");
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenCandidateTriesToWithdrawAnotherCandidatesApplication()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateEntity());

        var application = CreateApplication(ApplicationStage.Applied, _otherCandidateId); // Different candidate
        SetupApplicationsDbSet(application);

        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Bạn không có quyền rút hồ sơ này.");
    }

    #endregion

    #region Application Not Found Tests

    [Fact]
    public async Task Handle_ShouldThrowException_WhenCandidateProfileNotFound()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(null); // No candidate profile

        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("Không tìm thấy hồ sơ ứng viên.");
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenApplicationNotFound()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateEntity());
        SetupApplicationsDbSet(null); // No application

        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage($"*Không tìm thấy hồ sơ ứng tuyển với ID {_applicationId}*");
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenApplicationIsDeleted()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateEntity());

        var application = CreateApplication();
        application.IsDeleted = true;
        SetupApplicationsDbSet(application);

        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage($"*Không tìm thấy hồ sơ ứng tuyển với ID {_applicationId}*");
    }

    #endregion

    #region Business Rule Violation Tests - Terminal Stage Guard

    [Fact]
    public async Task Handle_ShouldThrowException_WhenApplicationIsAlreadyWithdrawn()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateEntity());

        var application = CreateApplication(ApplicationStage.Withdrawn);
        SetupApplicationsDbSet(application);

        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("*Không thể rút hồ sơ ứng tuyển*trạng thái kết thúc*");
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenApplicationIsRejected()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateEntity());

        var application = CreateApplication(ApplicationStage.Rejected);
        SetupApplicationsDbSet(application);

        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("*Không thể rút hồ sơ ứng tuyển*trạng thái kết thúc*");
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenApplicationIsHired()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateEntity());

        var application = CreateApplication(ApplicationStage.Hired);
        SetupApplicationsDbSet(application);

        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("*Không thể rút hồ sơ ứng tuyển*trạng thái kết thúc*");
    }

    #endregion

    #region Positive Scenarios - Happy Paths

    [Fact]
    public async Task Handle_ShouldSuccessfullyWithdrawApplication_WhenStageIsApplied()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateEntity());

        var application = CreateApplication(ApplicationStage.Applied);
        SetupApplicationsDbSet(application);

        var command = CreateValidCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ApplicationId.Should().Be(_applicationId);
        result.PreviousStage.Should().Be(ApplicationStage.Applied);
        result.NewStage.Should().Be(ApplicationStage.Withdrawn);
        result.WithdrawnAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_ShouldSuccessfullyWithdrawApplication_WhenStageIsShortlisted()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateEntity());

        var application = CreateApplication(ApplicationStage.Shortlisted);
        SetupApplicationsDbSet(application);

        var command = CreateValidCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.NewStage.Should().Be(ApplicationStage.Withdrawn);
        result.PreviousStage.Should().Be(ApplicationStage.Shortlisted);
    }

    [Fact]
    public async Task Handle_ShouldSuccessfullyWithdrawApplication_WhenStageIsInterviewScheduled()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateEntity());

        var application = CreateApplication(ApplicationStage.InterviewScheduled);
        SetupApplicationsDbSet(application);

        var command = CreateValidCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.NewStage.Should().Be(ApplicationStage.Withdrawn);
        result.PreviousStage.Should().Be(ApplicationStage.InterviewScheduled);
    }

    [Fact]
    public async Task Handle_ShouldSuccessfullyWithdrawApplication_WhenStageIsOffered()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateEntity());

        var application = CreateApplication(ApplicationStage.Offered);
        SetupApplicationsDbSet(application);

        var command = CreateValidCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.NewStage.Should().Be(ApplicationStage.Withdrawn);
        result.PreviousStage.Should().Be(ApplicationStage.Offered);
    }

    #endregion

    #region State Mutation Tests

    [Fact]
    public async Task Handle_ShouldUpdateApplicationStageToWithdrawn()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateEntity());

        var application = CreateApplication(ApplicationStage.Applied);
        SetupApplicationsDbSet(application);

        var command = CreateValidCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        application.Stage.Should().Be(ApplicationStage.Withdrawn);
    }

    [Fact]
    public async Task Handle_ShouldUpdateStatusToWithdrawn()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateEntity());

        var application = CreateApplication(ApplicationStage.Applied);
        SetupApplicationsDbSet(application);

        var command = CreateValidCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        application.Status.Should().Be("Withdrawn");
    }

    [Fact]
    public async Task Handle_ShouldUpdateStageUpdatedAtTimestamp()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateEntity());

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
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateEntity());

        var application = CreateApplication(ApplicationStage.Applied);
        application.UpdatedAt = DateTime.UtcNow.AddDays(-10);
        SetupApplicationsDbSet(application);

        var command = CreateValidCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        application.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_ShouldPersistWithdrawalReason_WhenProvided()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateEntity());

        var application = CreateApplication(ApplicationStage.Applied);
        SetupApplicationsDbSet(application);

        var command = CreateValidCommand("Đã nhận việc khác");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        application.RejectionReason.Should().Be("Đã nhận việc khác");
    }

    [Fact]
    public async Task Handle_ShouldTrimWithdrawalReason_WhenProvided()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateEntity());

        var application = CreateApplication(ApplicationStage.Applied);
        SetupApplicationsDbSet(application);

        var command = CreateValidCommand("  Đã nhận việc khác  ");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        application.RejectionReason.Should().Be("Đã nhận việc khác");
    }

    [Fact]
    public async Task Handle_ShouldSaveChangesToDatabase()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateEntity());

        var application = CreateApplication(ApplicationStage.Applied);
        SetupApplicationsDbSet(application);

        var command = CreateValidCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Return Value Tests

    [Fact]
    public async Task Handle_ShouldReturnCorrectApplicationId()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateEntity());

        var application = CreateApplication(ApplicationStage.Applied);
        SetupApplicationsDbSet(application);

        var command = CreateValidCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ApplicationId.Should().Be(_applicationId);
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectPreviousStage()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateEntity());

        var application = CreateApplication(ApplicationStage.Shortlisted);
        SetupApplicationsDbSet(application);

        var command = CreateValidCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.PreviousStage.Should().Be(ApplicationStage.Shortlisted);
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectNewStage()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateEntity());

        var application = CreateApplication(ApplicationStage.Applied);
        SetupApplicationsDbSet(application);

        var command = CreateValidCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.NewStage.Should().Be(ApplicationStage.Withdrawn);
    }

    #endregion
}
