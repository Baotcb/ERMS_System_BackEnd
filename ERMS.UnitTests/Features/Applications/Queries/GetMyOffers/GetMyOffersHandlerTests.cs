using ERMS.Application.Features.Applications.Queries.GetMyOffers;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Application;
using ERMS.Domain.Entities.Candidate;
using ERMS.Domain.Entities.Enterprise;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Organization;
using ERMS.Domain.Entities.Recruitment;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ERMS.UnitTests.Features.Applications.Queries.GetMyApplications;

using ApplicationEntity = ERMS.Domain.Entities.Application.Application;
using CandidateEntity = ERMS.Domain.Entities.Candidate.Candidate;

namespace ERMS.UnitTests.Features.Applications.Queries.GetMyOffers;

public class GetMyOffersHandlerTests
{
    private readonly Mock<IERMSDbContext> _contextMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<ILogger<GetMyOffersHandler>> _loggerMock;
    private readonly GetMyOffersHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _candidateId = Guid.NewGuid();
    private readonly Guid _enterpriseId = Guid.NewGuid();
    private readonly Guid _jobPostingId = Guid.NewGuid();
    private readonly Guid _applicationId = Guid.NewGuid();
    private readonly Guid _offerId = Guid.NewGuid();

    public GetMyOffersHandlerTests()
    {
        _contextMock = new Mock<IERMSDbContext>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _loggerMock = new Mock<ILogger<GetMyOffersHandler>>();

        _handler = new GetMyOffersHandler(
            _contextMock.Object,
            _currentUserServiceMock.Object,
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
                FullName = "Candidate",
                Email = "candidate@test.com"
            }
        };
    }

    private Enterprise CreateEnterprise()
    {
        return new Enterprise
        {
            Id = _enterpriseId,
            EnterpriseName = "ERMS Company",
            EnterpriseCode = "ERMS",
            LogoUrl = "https://cdn.example.com/logo.png",
            IsDeleted = false
        };
    }

    private Department CreateDepartment()
    {
        return new Department
        {
            Id = 1,
            EnterpriseId = _enterpriseId,
            DepartmentName = "Engineering",
            IsDeleted = false
        };
    }

    private JobPosting CreateJobPosting(Enterprise enterprise)
    {
        return new JobPosting
        {
            Id = _jobPostingId,
            EnterpriseId = enterprise.Id,
            Enterprise = enterprise,
            DepartmentId = 1,
            JobTitle = "Senior Backend Engineer",
            JobCode = "SBE-001",
            Description = "Build backend services",
            EmploymentType = "FullTime",
            Status = "Published",
            IsDeleted = false
        };
    }

    private ApplicationEntity CreateApplication(
        JobPosting jobPosting,
        Guid? candidateId = null,
        Guid? applicationId = null)
    {
        return new ApplicationEntity
        {
            Id = applicationId ?? _applicationId,
            CandidateId = candidateId ?? _candidateId,
            JobPostingId = jobPosting.Id,
            JobPosting = jobPosting,
            Stage = "Offered",
            Status = "Active",
            AppliedAt = DateTime.UtcNow.AddDays(-7),
            IsDeleted = false
        };
    }

    private Offer CreateOffer(ApplicationEntity application, Department department, Guid? offerId = null)
    {
        return new Offer
        {
            Id = offerId ?? _offerId,
            ApplicationId = application.Id,
            Application = application,
            OfferCode = "OFF-001",
            Position = "Senior Backend Engineer",
            DepartmentId = department.Id,
            Department = department,
            Salary = 30000000,
            SalaryFrequency = "Monthly",
            StartDate = DateTime.UtcNow.AddDays(14),
            ExpirationDate = DateTime.UtcNow.AddDays(21),
            OfferLetterUrl = "https://cdn.example.com/offers/off-001.pdf",
            Status = "Sent",
            CreatedById = Guid.NewGuid(),
            CandidateNote = "Welcome aboard",
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    private void SetupCandidatesDbSet(CandidateEntity candidate)
    {
        var mockSet = CreateMockDbSet(new List<CandidateEntity> { candidate }.AsQueryable());
        _contextMock.Setup(c => c.Candidates).Returns(mockSet.Object);
    }

    private void SetupOffersDbSet(IEnumerable<Offer> offers)
    {
        var mockSet = CreateMockDbSet(offers.AsQueryable());
        _contextMock.Setup(c => c.Offers).Returns(mockSet.Object);
    }

    [Fact]
    public async Task Handle_ShouldPopulateEnterpriseFields_WhenCandidateHasOffers()
    {
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateProfile());

        var enterprise = CreateEnterprise();
        var department = CreateDepartment();
        var jobPosting = CreateJobPosting(enterprise);
        var application = CreateApplication(jobPosting);
        var offer = CreateOffer(application, department);

        SetupOffersDbSet([offer]);

        var result = await _handler.Handle(new GetMyOffersQuery(), CancellationToken.None);

        result.Items.Should().ContainSingle();

        var item = result.Items[0];
        item.OfferId.Should().Be(_offerId);
        item.ApplicationId.Should().Be(_applicationId);
        item.JobPostingId.Should().Be(_jobPostingId);
        item.EnterpriseId.Should().Be(_enterpriseId);
        item.EnterpriseName.Should().Be("ERMS Company");
        item.EnterpriseLogoUrl.Should().Be("https://cdn.example.com/logo.png");
        item.DepartmentName.Should().Be("Engineering");
        item.JobTitle.Should().Be("Senior Backend Engineer");
    }

    [Fact]
    public async Task Handle_ShouldOnlyReturnOffersBelongingToCurrentCandidate()
    {
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidateProfile());

        var enterprise = CreateEnterprise();
        var department = CreateDepartment();
        var jobPosting = CreateJobPosting(enterprise);

        var ownOffer = CreateOffer(
            CreateApplication(jobPosting, applicationId: Guid.NewGuid()),
            department,
            Guid.NewGuid());
        var otherOffer = CreateOffer(
            CreateApplication(jobPosting, Guid.NewGuid(), Guid.NewGuid()),
            department,
            Guid.NewGuid());

        SetupOffersDbSet([ownOffer, otherOffer]);

        var result = await _handler.Handle(new GetMyOffersQuery(), CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items[0].OfferId.Should().Be(ownOffer.Id);
    }
}
