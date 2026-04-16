using ERMS.Application.Features.Applications.Commands.RespondOfferByToken;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

using ApplicationEntity = ERMS.Domain.Entities.Application.Application;
using OfferEntity = ERMS.Domain.Entities.Application.Offer;

namespace ERMS.UnitTests.Features.Applications.Commands.RespondOfferByToken;

public class RespondOfferByTokenHandlerTests
{
    private readonly Mock<IERMSDbContext> _contextMock;
    private readonly Mock<ILogger<RespondOfferByTokenHandler>> _loggerMock;
    private readonly RespondOfferByTokenHandler _handler;

    public RespondOfferByTokenHandlerTests()
    {
        _contextMock = new Mock<IERMSDbContext>();
        _loggerMock = new Mock<ILogger<RespondOfferByTokenHandler>>();

        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new RespondOfferByTokenHandler(_contextMock.Object, _loggerMock.Object);
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

    private static OfferEntity CreateOffer(string token, DateTime? tokenExpiresAt = null)
    {
        return new OfferEntity
        {
            Id = Guid.NewGuid(),
            ApplicationId = Guid.NewGuid(),
            Position = "Backend Engineer",
            DepartmentId = 1,
            Salary = 2000,
            StartDate = DateTime.UtcNow.AddMonths(1),
            ExpirationDate = DateTime.UtcNow.AddDays(5),
            Status = OfferStatus.Sent,
            CreatedById = Guid.NewGuid(),
            ResponseToken = token,
            TokenExpiresAt = tokenExpiresAt ?? DateTime.UtcNow.AddDays(2),
            IsDeleted = false,
            Application = new ApplicationEntity
            {
                Id = Guid.NewGuid(),
                JobPostingId = Guid.NewGuid(),
                CandidateId = Guid.NewGuid(),
                Stage = ApplicationStage.Offered,
                Status = "Active",
                IsDeleted = false
            }
        };
    }

    [Fact]
    public async Task Handle_ShouldAcceptOffer_WhenActionIsAccept()
    {
        // Arrange
        var token = "token-accept";
        var offer = CreateOffer(token);

        _contextMock.Setup(c => c.Offers)
            .Returns(CreateMockDbSet(new List<OfferEntity> { offer }.AsQueryable()).Object);

        var command = new RespondOfferByTokenCommand
        {
            Token = token,
            Action = "accept"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.NewOfferStatus.Should().Be(OfferStatus.Accepted);
        offer.Status.Should().Be(OfferStatus.Accepted);
        offer.ResponseToken.Should().BeNull();
        offer.TokenExpiresAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldRejectOffer_AndApplication_WhenActionIsReject()
    {
        // Arrange
        var token = "token-reject";
        var offer = CreateOffer(token);

        _contextMock.Setup(c => c.Offers)
            .Returns(CreateMockDbSet(new List<OfferEntity> { offer }.AsQueryable()).Object);

        var command = new RespondOfferByTokenCommand
        {
            Token = token,
            Action = "reject"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.NewOfferStatus.Should().Be(OfferStatus.Rejected);
        offer.Status.Should().Be(OfferStatus.Rejected);
        offer.Application.Stage.Should().Be(ApplicationStage.Rejected);
        offer.ResponseToken.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenTokenExpired()
    {
        // Arrange
        var token = "token-expired";
        var offer = CreateOffer(token, DateTime.UtcNow.AddMinutes(-10));

        _contextMock.Setup(c => c.Offers)
            .Returns(CreateMockDbSet(new List<OfferEntity> { offer }.AsQueryable()).Object);

        var command = new RespondOfferByTokenCommand
        {
            Token = token,
            Action = "accept"
        };

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("*hết hạn*");
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenTokenNotFoundOrUsed()
    {
        // Arrange
        _contextMock.Setup(c => c.Offers)
            .Returns(CreateMockDbSet(new List<OfferEntity>().AsQueryable()).Object);

        var command = new RespondOfferByTokenCommand
        {
            Token = "used-token",
            Action = "accept"
        };

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("*token*không hợp lệ*");
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenActionIsInvalid()
    {
        // Arrange
        var token = "token-invalid-action";
        var offer = CreateOffer(token);

        _contextMock.Setup(c => c.Offers)
            .Returns(CreateMockDbSet(new List<OfferEntity> { offer }.AsQueryable()).Object);

        var command = new RespondOfferByTokenCommand
        {
            Token = token,
            Action = "maybe"
        };

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("*Action không hợp lệ*");
    }
}
