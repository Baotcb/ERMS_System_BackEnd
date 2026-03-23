using ERMS.Application.Features.Applications.Commands.RejectOffer;
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

namespace ERMS.UnitTests.Features.Applications.Commands.RejectOffer;

public class RejectOfferCommandHandlerTests
{
    private readonly Mock<IERMSDbContext> _contextMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<ILogger<RejectOfferCommandHandler>> _loggerMock;
    private readonly RejectOfferCommandHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _candidateId = Guid.NewGuid();
    private readonly Guid _offerId = Guid.NewGuid();
    private readonly Guid _applicationId = Guid.NewGuid();
    private readonly Guid _jobPostingId = Guid.NewGuid();
    private readonly Guid _otherCandidateId = Guid.NewGuid();

    public RejectOfferCommandHandlerTests()
    {
        _contextMock = new Mock<IERMSDbContext>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _loggerMock = new Mock<ILogger<RejectOfferCommandHandler>>();

        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new RejectOfferCommandHandler(
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

    private OfferEntity CreateOffer(string status = "Sent", Guid? candidateId = null)
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
            ExpirationDate = DateTime.UtcNow.AddDays(7),
            Status = status,
            CreatedById = Guid.NewGuid(),
            IsDeleted = false
        };
    }

    private RejectOfferCommand CreateValidCommand(string candidateNote = "I found a better opportunity.")
    {
        return new RejectOfferCommand
        {
            OfferId = _offerId,
            CandidateNote = candidateNote
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
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);
        var command = CreateValidCommand();

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Người dùng chưa được xác thực.");
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserIsNotCandidate()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(x => x.Roles).Returns([AppRoles.HRManager]);
        var command = CreateValidCommand();

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Chỉ ứng viên mới có quyền từ chối đề nghị.");
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenCandidateTriesToRejectAnotherCandidatesOffer()
    {
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateEntity());

        var offer = CreateOffer(candidateId: _otherCandidateId);
        SetupOffersDbSet(offer);

        var command = CreateValidCommand();

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Bạn không có quyền truy cập đề nghị này.");
    }

    #endregion

    #region Not Found Tests

    [Fact]
    public async Task Handle_ShouldThrowException_WhenCandidateProfileNotFound()
    {
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(null);
        var command = CreateValidCommand();

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("Không tìm thấy hồ sơ ứng viên.");
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenOfferNotFound()
    {
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateEntity());
        SetupOffersDbSet(null);
        var command = CreateValidCommand();

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage($"*Không tìm thấy đề nghị với ID {_offerId}*");
    }

    #endregion

    #region Business Rule Violation Tests

    [Fact]
    public async Task Handle_ShouldThrowException_WhenOfferStatusIsNotSent()
    {
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateEntity());

        var offer = CreateOffer(status: "Draft");
        SetupOffersDbSet(offer);

        var command = CreateValidCommand();

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("*Đề nghị này không thể bị từ chối*Chỉ đề nghị có trạng thái 'Sent' mới có thể từ chối*");
    }

    #endregion

    #region Happy Path & State Mutation Tests

    [Fact]
    public async Task Handle_ShouldSuccessfullyRejectOffer()
    {
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateEntity());

        var offer = CreateOffer();
        SetupOffersDbSet(offer);

        var command = CreateValidCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        result.OfferId.Should().Be(_offerId);
        result.NewOfferStatus.Should().Be(OfferStatus.Rejected);
        result.RespondedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_ShouldSetOfferStatusToRejected()
    {
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateEntity());

        var offer = CreateOffer();
        SetupOffersDbSet(offer);

        await _handler.Handle(CreateValidCommand(), CancellationToken.None);

        offer.Status.Should().Be(OfferStatus.Rejected);
        offer.RespondedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_ShouldChangeApplicationStageToRejected()
    {
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateEntity());

        var offer = CreateOffer();
        SetupOffersDbSet(offer);

        await _handler.Handle(CreateValidCommand(), CancellationToken.None);

        offer.Application.Stage.Should().Be(ApplicationStage.Rejected);
        offer.Application.RejectedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        offer.Application.RejectedById.Should().Be(_userId);
    }

    [Fact]
    public async Task Handle_ShouldPersistCandidateNote()
    {
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateEntity());

        var offer = CreateOffer();
        SetupOffersDbSet(offer);

        await _handler.Handle(CreateValidCommand("I found a better opportunity."), CancellationToken.None);

        offer.CandidateNote.Should().Be("I found a better opportunity.");
        offer.Application.RejectionReason.Should().Be("I found a better opportunity.");
    }

    [Fact]
    public async Task Handle_ShouldTrimCandidateNote()
    {
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateEntity());

        var offer = CreateOffer();
        SetupOffersDbSet(offer);

        await _handler.Handle(CreateValidCommand("  Salary does not align.  "), CancellationToken.None);

        offer.CandidateNote.Should().Be("Salary does not align.");
        offer.Application.RejectionReason.Should().Be("Salary does not align.");
    }

    [Fact]
    public async Task Handle_ShouldSaveChangesToDatabase()
    {
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateEntity());

        var offer = CreateOffer();
        SetupOffersDbSet(offer);

        await _handler.Handle(CreateValidCommand(), CancellationToken.None);

        _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
