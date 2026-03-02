using ERMS.Application.Features.Applications.Commands.AcceptOffer;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Candidate;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

using ApplicationEntity = ERMS.Domain.Entities.Application.Application;
using OfferEntity = ERMS.Domain.Entities.Application.Offer;

namespace ERMS.UnitTests.Features.Applications.Commands.AcceptOffer;

public class AcceptOfferCommandHandlerTests
{
    private readonly Mock<IERMSDbContext> _contextMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<ILogger<AcceptOfferCommandHandler>> _loggerMock;
    private readonly AcceptOfferCommandHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _candidateId = Guid.NewGuid();
    private readonly Guid _offerId = Guid.NewGuid();
    private readonly Guid _applicationId = Guid.NewGuid();
    private readonly Guid _jobPostingId = Guid.NewGuid();
    private readonly Guid _otherCandidateId = Guid.NewGuid();

    public AcceptOfferCommandHandlerTests()
    {
        _contextMock = new Mock<IERMSDbContext>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _loggerMock = new Mock<ILogger<AcceptOfferCommandHandler>>();

        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new AcceptOfferCommandHandler(
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

    private OfferEntity CreateOffer(string status = "Sent", Guid? candidateId = null, DateTime? expirationDate = null)
    {
        var application = new ApplicationEntity
        {
            Id = _applicationId,
            JobPostingId = _jobPostingId,
            CandidateId = candidateId ?? _candidateId,
            Stage = ApplicationStage.Offered,
            Status = "Active",
            AppliedAt = DateTime.UtcNow.AddDays(-10),
            IsDeleted = false
        };

        return new OfferEntity
        {
            Id = _offerId,
            ApplicationId = _applicationId,
            Application = application,
            Position = "Software Engineer",
            DepartmentId = 1,
            Salary = 5000,
            SalaryFrequency = "Monthly",
            StartDate = DateTime.UtcNow.AddMonths(1),
            ExpirationDate = expirationDate ?? DateTime.UtcNow.AddDays(7),
            Status = status,
            CreatedById = Guid.NewGuid(),
            IsDeleted = false
        };
    }

    private AcceptOfferCommand CreateValidCommand()
    {
        return new AcceptOfferCommand { OfferId = _offerId };
    }

    private void SetupCandidatesDbSet(Candidate? candidate)
    {
        var data = candidate != null
            ? new List<Candidate> { candidate }.AsQueryable()
            : new List<Candidate>().AsQueryable();

        var mockSet = CreateMockDbSet(data);
        _contextMock.Setup(c => c.Candidates).Returns(mockSet.Object);
    }

    private void SetupOffersDbSet(OfferEntity? offer)
    {
        var data = offer != null
            ? new List<OfferEntity> { offer }.AsQueryable()
            : new List<OfferEntity>().AsQueryable();

        var mockSet = CreateMockDbSet(data);
        _contextMock.Setup(c => c.Offers).Returns(mockSet.Object);
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
    public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserIsNotCandidate()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(x => x.Roles).Returns([AppRoles.HRManager]);
        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Only candidates can accept offers.");
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
            .WithMessage("Only candidates can accept offers.");
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenCandidateTriesToAcceptAnotherCandidatesOffer()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateEntity());

        var offer = CreateOffer(candidateId: _otherCandidateId);
        SetupOffersDbSet(offer);

        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("You do not have permission to access this offer.");
    }

    #endregion

    #region Not Found Tests

    [Fact]
    public async Task Handle_ShouldThrowException_WhenCandidateProfileNotFound()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(null);
        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("Candidate profile not found.");
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenOfferNotFound()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateEntity());
        SetupOffersDbSet(null);
        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage($"*Offer with ID {_offerId} not found*");
    }

    #endregion

    #region Business Rule Violation Tests

    [Fact]
    public async Task Handle_ShouldThrowException_WhenOfferStatusIsNotSent()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateEntity());

        var offer = CreateOffer(status: "Draft");
        SetupOffersDbSet(offer);

        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("*Cannot accept offer*Only offers with status 'Sent' can be accepted*");
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenOfferIsExpired()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateEntity());

        var offer = CreateOffer(expirationDate: DateTime.UtcNow.AddDays(-1));
        SetupOffersDbSet(offer);

        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("*Cannot accept offer*expired*");
    }

    #endregion

    #region Happy Path & State Mutation Tests

    [Fact]
    public async Task Handle_ShouldSuccessfullyAcceptOffer()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateEntity());

        var offer = CreateOffer();
        SetupOffersDbSet(offer);

        var command = CreateValidCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.OfferId.Should().Be(_offerId);
        result.NewOfferStatus.Should().Be(OfferStatus.Accepted);
        result.ApplicationStage.Should().Be(ApplicationStage.Hired);
        result.RespondedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_ShouldSetOfferStatusToAccepted()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateEntity());

        var offer = CreateOffer();
        SetupOffersDbSet(offer);

        var command = CreateValidCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        offer.Status.Should().Be(OfferStatus.Accepted);
        offer.RespondedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_ShouldUpdateApplicationStageToHired()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateEntity());

        var offer = CreateOffer();
        SetupOffersDbSet(offer);

        var command = CreateValidCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        offer.Application.Stage.Should().Be(ApplicationStage.Hired);
        offer.Application.StageUpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_ShouldSaveChangesToDatabase()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateEntity());

        var offer = CreateOffer();
        SetupOffersDbSet(offer);

        var command = CreateValidCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
