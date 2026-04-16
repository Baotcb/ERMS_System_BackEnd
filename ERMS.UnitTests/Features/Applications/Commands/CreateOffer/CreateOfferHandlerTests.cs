using ERMS.Application.Features.Applications.Commands.CreateOffer;
using ERMS.Application.Exceptions;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Candidate;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Recruitment;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

using ApplicationEntity = ERMS.Domain.Entities.Application.Application;
using OfferEntity = ERMS.Domain.Entities.Application.Offer;

namespace ERMS.UnitTests.Features.Applications.Commands.CreateOffer;

public class CreateOfferHandlerTests
{
    private readonly Mock<IERMSDbContext> _contextMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IDbContextTransaction> _transactionMock;
    private readonly IConfiguration _configuration;

    private readonly Guid _currentUserId = Guid.NewGuid();
    private readonly Guid _enterpriseId = Guid.NewGuid();
    private readonly Guid _applicationId = Guid.NewGuid();

    public CreateOfferHandlerTests()
    {
        _contextMock = new Mock<IERMSDbContext>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _emailServiceMock = new Mock<IEmailService>();
        _transactionMock = new Mock<IDbContextTransaction>();

        var store = new Mock<IUserStore<User>>();
        _userManagerMock = new Mock<UserManager<User>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ClientSettings:Url"] = "http://localhost:3000"
            })
            .Build();

        _contextMock.Setup(c => c.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);
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
        _currentUserServiceMock.Setup(x => x.GetDepartmentIdAsync()).ReturnsAsync(1);
    }

    private static CreateOfferCommand CreateCommand(Guid applicationId)
    {
        return new CreateOfferCommand
        {
            ApplicationId = applicationId,
            Position = "Backend Engineer",
            Salary = 3000,
            SalaryFrequency = "Monthly",
            Bonus = "Performance bonus",
            Benefits = "Health insurance",
            StartDate = DateTime.UtcNow.AddDays(10),
            ExpirationDate = DateTime.UtcNow.AddDays(5),
            OfferLetterUrl = "https://example.com/offer.pdf"
        };
    }

    private ApplicationEntity CreateApplication(bool isExternal)
    {
        var candidateUser = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Internal Candidate",
            Email = "internal.candidate@gmail.com",
            PhoneNumber = "0900000000"
        };

        var application = new ApplicationEntity
        {
            Id = _applicationId,
            JobPostingId = Guid.NewGuid(),
            CandidateId = Guid.NewGuid(),
            Candidate = new Candidate
            {
                Id = Guid.NewGuid(),
                UserId = candidateUser.Id,
                User = candidateUser,
                IsDeleted = false
            },
            JobPosting = new JobPosting
            {
                Id = Guid.NewGuid(),
                EnterpriseId = _enterpriseId,
                DepartmentId = 1,
                Description = "Backend position",
                JobTitle = "Backend Engineer",
                IsDeleted = false
            },
            Stage = ApplicationStage.OfferProcessing,
            Status = "Active",
            IsDeleted = false
        };

        if (isExternal)
        {
            var external = new ExternalCandidate
            {
                Id = Guid.NewGuid(),
                FullName = "External Candidate",
                Email = "external.candidate@gmail.com",
                PhoneNumber = "0912345678",
                EnterpriseId = _enterpriseId,
                CreatedById = _currentUserId,
                Source = "HRImported"
            };

            application.ExternalCandidateId = external.Id;
            application.ExternalCandidate = external;
        }

        return application;
    }

    [Fact]
    public async Task Handle_ShouldGenerateTokenAndSendResponseLinks_ForExternalApplication()
    {
        // Arrange
        SetupAuthenticatedHr();
        var application = CreateApplication(isExternal: true);

        var offers = new List<OfferEntity>();
        _contextMock.Setup(c => c.Offers).Returns(CreateMockDbSet(offers.AsQueryable()).Object);
        _contextMock.Setup(c => c.Applications).Returns(CreateMockDbSet(new List<ApplicationEntity> { application }.AsQueryable()).Object);

        OfferEntity? addedOffer = null;
        _contextMock.Setup(c => c.Offers.AddAsync(It.IsAny<OfferEntity>(), It.IsAny<CancellationToken>()))
            .Callback<OfferEntity, CancellationToken>((o, _) =>
            {
                addedOffer = o;
                offers.Add(o);
            });
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new CreateOfferHandler(
            _contextMock.Object,
            _currentUserServiceMock.Object,
            _emailServiceMock.Object,
            _configuration,
            _userManagerMock.Object);

        var command = CreateCommand(_applicationId);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        addedOffer.Should().NotBeNull();
        addedOffer!.ResponseToken.Should().NotBeNullOrWhiteSpace();
        addedOffer.TokenExpiresAt.Should().Be(command.ExpirationDate);

        _emailServiceMock.Verify(x => x.SendEmailAsync(
                "external.candidate@gmail.com",
                It.IsAny<string>(),
                It.Is<string>(body =>
                    body.Contains($"/offer-response/{addedOffer.ResponseToken}?action=accept") &&
                    body.Contains($"/offer-response/{addedOffer.ResponseToken}?action=reject"))),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldNotGenerateToken_ForInternalApplication()
    {
        // Arrange
        SetupAuthenticatedHr();
        var application = CreateApplication(isExternal: false);

        var offers = new List<OfferEntity>();
        _contextMock.Setup(c => c.Offers).Returns(CreateMockDbSet(offers.AsQueryable()).Object);
        _contextMock.Setup(c => c.Applications).Returns(CreateMockDbSet(new List<ApplicationEntity> { application }.AsQueryable()).Object);

        OfferEntity? addedOffer = null;
        _contextMock.Setup(c => c.Offers.AddAsync(It.IsAny<OfferEntity>(), It.IsAny<CancellationToken>()))
            .Callback<OfferEntity, CancellationToken>((o, _) =>
            {
                addedOffer = o;
                offers.Add(o);
            });
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new CreateOfferHandler(
            _contextMock.Object,
            _currentUserServiceMock.Object,
            _emailServiceMock.Object,
            _configuration,
            _userManagerMock.Object);

        var command = CreateCommand(_applicationId);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        addedOffer.Should().NotBeNull();
        addedOffer!.ResponseToken.Should().BeNull();
        addedOffer.TokenExpiresAt.Should().BeNull();

        _emailServiceMock.Verify(x => x.SendEmailAsync(
            "internal.candidate@gmail.com",
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowBusinessException_WhenExpirationDateIsOnOrAfterStartDate()
    {
        // Arrange
        SetupAuthenticatedHr();
        var application = CreateApplication(isExternal: false);

        var offers = new List<OfferEntity>();
        _contextMock.Setup(c => c.Offers).Returns(CreateMockDbSet(offers.AsQueryable()).Object);
        _contextMock.Setup(c => c.Applications).Returns(CreateMockDbSet(new List<ApplicationEntity> { application }.AsQueryable()).Object);

        var handler = new CreateOfferHandler(
            _contextMock.Object,
            _currentUserServiceMock.Object,
            _emailServiceMock.Object,
            _configuration,
            _userManagerMock.Object);

        var command = CreateCommand(_applicationId);
        command.StartDate = DateTime.UtcNow.AddDays(5);
        command.ExpirationDate = command.StartDate;

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Hạn phản hồi offer phải trước ngày bắt đầu làm việc.");
    }

    [Fact]
    public async Task Handle_ShouldReuseSoftDeletedOffer_InsteadOfAddingNewRow()
    {
        // Arrange
        SetupAuthenticatedHr();
        var application = CreateApplication(isExternal: false);

        var softDeletedOffer = new OfferEntity
        {
            Id = Guid.NewGuid(),
            ApplicationId = _applicationId,
            OfferCode = "OFF2026040001",
            Position = "Old Position",
            DepartmentId = 1,
            Salary = 1000,
            SalaryFrequency = "Monthly",
            StartDate = DateTime.UtcNow.AddDays(3),
            ExpirationDate = DateTime.UtcNow.AddDays(1),
            Status = OfferStatus.Cancelled,
            CreatedById = _currentUserId,
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        application.Offer = softDeletedOffer;

        var offers = new List<OfferEntity> { softDeletedOffer };
        _contextMock.Setup(c => c.Offers).Returns(CreateMockDbSet(offers.AsQueryable()).Object);
        _contextMock.Setup(c => c.Applications).Returns(CreateMockDbSet(new List<ApplicationEntity> { application }.AsQueryable()).Object);
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new CreateOfferHandler(
            _contextMock.Object,
            _currentUserServiceMock.Object,
            _emailServiceMock.Object,
            _configuration,
            _userManagerMock.Object);

        var command = CreateCommand(_applicationId);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        softDeletedOffer.IsDeleted.Should().BeFalse();
        softDeletedOffer.DeletedAt.Should().BeNull();
        softDeletedOffer.Status.Should().Be(OfferStatus.Sent);
        softDeletedOffer.Position.Should().Be(command.Position);
        softDeletedOffer.Salary.Should().Be(command.Salary);
        softDeletedOffer.StartDate.Should().Be(command.StartDate);
        softDeletedOffer.ExpirationDate.Should().Be(command.ExpirationDate);

        _contextMock.Verify(
            c => c.Offers.AddAsync(It.IsAny<OfferEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldNotCreateOffer_WhenEmailSendingFails()
    {
        // Arrange
        SetupAuthenticatedHr();
        var application = CreateApplication(isExternal: false);

        var offers = new List<OfferEntity>();
        _contextMock.Setup(c => c.Offers).Returns(CreateMockDbSet(offers.AsQueryable()).Object);
        _contextMock.Setup(c => c.Applications).Returns(CreateMockDbSet(new List<ApplicationEntity> { application }.AsQueryable()).Object);

        _contextMock.Setup(c => c.Offers.AddAsync(It.IsAny<OfferEntity>(), It.IsAny<CancellationToken>()))
            .Callback<OfferEntity, CancellationToken>((o, _) => offers.Add(o));

        _emailServiceMock.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("SMTP down"));

        var handler = new CreateOfferHandler(
            _contextMock.Object,
            _currentUserServiceMock.Object,
            _emailServiceMock.Object,
            _configuration,
            _userManagerMock.Object);

        var command = CreateCommand(_applicationId);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Gửi email offer thất bại:*");

        offers.Should().BeEmpty();
        _contextMock.Verify(c => c.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _transactionMock.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
        _transactionMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _contextMock.Verify(c => c.Offers.AddAsync(It.IsAny<OfferEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
