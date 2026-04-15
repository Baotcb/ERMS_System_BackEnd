using ERMS.Application.Features.Applications.Commands.AddExternalApplication;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Constants.Recruitment;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Candidate;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Recruitment;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

using ApplicationEntity = ERMS.Domain.Entities.Application.Application;

namespace ERMS.UnitTests.Features.Applications.Commands.AddExternalApplication;

public class AddExternalApplicationHandlerTests
{
    private readonly Mock<IERMSDbContext> _contextMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<ISubscriptionLimitChecker> _subscriptionLimitCheckerMock;
    private readonly Mock<IBackgroundTaskQueue> _backgroundTaskQueueMock;
    private readonly Mock<ILogger<AddExternalApplicationHandler>> _loggerMock;

    private readonly AddExternalApplicationHandler _handler;

    private readonly Guid _currentUserId = Guid.NewGuid();
    private readonly Guid _enterpriseId = Guid.NewGuid();
    private readonly Guid _jobPostingId = Guid.NewGuid();

    public AddExternalApplicationHandlerTests()
    {
        _contextMock = new Mock<IERMSDbContext>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _subscriptionLimitCheckerMock = new Mock<ISubscriptionLimitChecker>();
        _backgroundTaskQueueMock = new Mock<IBackgroundTaskQueue>();
        _loggerMock = new Mock<ILogger<AddExternalApplicationHandler>>();

        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _subscriptionLimitCheckerMock
            .Setup(x => x.IsProPlanAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _handler = new AddExternalApplicationHandler(
            _contextMock.Object,
            _currentUserServiceMock.Object,
            _subscriptionLimitCheckerMock.Object,
            _backgroundTaskQueueMock.Object,
            _loggerMock.Object);
    }

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

    private void SetupAuthenticatedHr()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_currentUserId);
        _currentUserServiceMock.Setup(x => x.Roles).Returns([AppRoles.HRManager]);
        _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(_enterpriseId);
    }

    private void SetupJobPosting(JobPosting? jobPosting = null)
    {
        var posting = jobPosting ?? new JobPosting
        {
            Id = _jobPostingId,
            EnterpriseId = _enterpriseId,
            DepartmentId = 1,
            JobTitle = "Backend Engineer",
            Description = "Build APIs",
            Requirements = "C#, .NET",
            Status = JobPostingStatus.Published,
            IsDeleted = false
        };

        var mockSet = CreateMockDbSet(new List<JobPosting> { posting }.AsQueryable());
        _contextMock.Setup(c => c.JobPostings).Returns(mockSet.Object);
    }

    private static AddExternalApplicationCommand CreateCommand(Guid jobPostingId, string email = "external.candidate@gmail.com")
    {
        return new AddExternalApplicationCommand
        {
            JobPostingId = jobPostingId,
            CandidateName = "External Candidate",
            CandidateEmail = email,
            CandidatePhone = "0912345678",
            ResumeUrl = "https://res.cloudinary.com/erms/cv.pdf",
            ResumeText = "Experienced .NET engineer"
        };
    }

    [Fact]
    public async Task Handle_ShouldCreateExternalCandidate_Application_AndResume()
    {
        // Arrange
        SetupAuthenticatedHr();
        SetupJobPosting();

        var applications = new List<ApplicationEntity>();
        var users = new List<User>();
        var candidates = new List<Candidate>();
        var resumes = new List<Resume>();
        var externalCandidates = new List<ExternalCandidate>();

        _contextMock.Setup(c => c.Applications).Returns(CreateMockDbSet(applications.AsQueryable()).Object);
        _contextMock.Setup(c => c.Users).Returns(CreateMockDbSet(users.AsQueryable()).Object);
        _contextMock.Setup(c => c.Candidates).Returns(CreateMockDbSet(candidates.AsQueryable()).Object);
        _contextMock.Setup(c => c.Resumes).Returns(CreateMockDbSet(resumes.AsQueryable()).Object);
        _contextMock.Setup(c => c.ExternalCandidates).Returns(CreateMockDbSet(externalCandidates.AsQueryable()).Object);

        _contextMock.Setup(c => c.Users.Add(It.IsAny<User>())).Callback<User>(u => users.Add(u));
        _contextMock.Setup(c => c.Candidates.Add(It.IsAny<Candidate>())).Callback<Candidate>(u => candidates.Add(u));
        _contextMock.Setup(c => c.Resumes.Add(It.IsAny<Resume>())).Callback<Resume>(u => resumes.Add(u));
        _contextMock.Setup(c => c.Applications.Add(It.IsAny<ApplicationEntity>())).Callback<ApplicationEntity>(u => applications.Add(u));
        _contextMock.Setup(c => c.ExternalCandidates.Add(It.IsAny<ExternalCandidate>())).Callback<ExternalCandidate>(u => externalCandidates.Add(u));

        var command = CreateCommand(_jobPostingId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Stage.Should().Be(ApplicationStage.Applied);
        result.ApplicationId.Should().NotBeEmpty();

        externalCandidates.Should().ContainSingle();
        externalCandidates[0].Email.Should().Be(command.CandidateEmail);
        externalCandidates[0].FullName.Should().Be(command.CandidateName);

        candidates.Should().ContainSingle();
        resumes.Should().ContainSingle();
        applications.Should().ContainSingle();

        applications[0].ExternalCandidateId.Should().Be(externalCandidates[0].Id);
        applications[0].Source.Should().Be("HRImported");
        resumes[0].FileUrl.Should().Be(command.ResumeUrl);

        _backgroundTaskQueueMock.Verify(x => x.EnqueueAsync(It.IsAny<CvScoringWorkItem>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenEmailAlreadyExistsInSystem()
    {
        // Arrange
        SetupAuthenticatedHr();
        SetupJobPosting();

        var email = "existing.user@gmail.com";

        var users = new List<User>
        {
            new() { Id = Guid.NewGuid(), Email = email, UserName = email }
        };

        _contextMock.Setup(c => c.Users).Returns(CreateMockDbSet(users.AsQueryable()).Object);
        _contextMock.Setup(c => c.Applications).Returns(CreateMockDbSet(new List<ApplicationEntity>().AsQueryable()).Object);
        _contextMock.Setup(c => c.ExternalCandidates).Returns(CreateMockDbSet(new List<ExternalCandidate>().AsQueryable()).Object);

        var command = CreateCommand(_jobPostingId, email);

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("*Email*đã tồn tại*");
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenDuplicateApplicationByJobPostingAndEmail()
    {
        // Arrange
        SetupAuthenticatedHr();
        SetupJobPosting();

        var email = "duplicate.external@gmail.com";

        var existingExternal = new ExternalCandidate
        {
            Id = Guid.NewGuid(),
            FullName = "Already Exists",
            Email = email,
            EnterpriseId = _enterpriseId,
            CreatedById = _currentUserId,
            Source = "HRImported"
        };

        var existingApplication = new ApplicationEntity
        {
            Id = Guid.NewGuid(),
            JobPostingId = _jobPostingId,
            CandidateId = Guid.NewGuid(),
            ExternalCandidateId = existingExternal.Id,
            ExternalCandidate = existingExternal,
            Stage = ApplicationStage.Applied,
            Status = "Active",
            IsDeleted = false
        };

        _contextMock.Setup(c => c.Applications)
            .Returns(CreateMockDbSet(new List<ApplicationEntity> { existingApplication }.AsQueryable()).Object);
        _contextMock.Setup(c => c.Users)
            .Returns(CreateMockDbSet(new List<User>().AsQueryable()).Object);
        _contextMock.Setup(c => c.ExternalCandidates)
            .Returns(CreateMockDbSet(new List<ExternalCandidate> { existingExternal }.AsQueryable()).Object);

        var command = CreateCommand(_jobPostingId, email);

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("*đã tồn tại*");
    }
}
