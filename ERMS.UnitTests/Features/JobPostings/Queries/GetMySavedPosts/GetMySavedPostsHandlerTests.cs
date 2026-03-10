using ERMS.Application.Features.JobPostings.Queries.GetMySavedPosts;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Candidate;
using ERMS.Domain.Entities.Enterprise;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Recruitment;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

using CandidateEntity = ERMS.Domain.Entities.Candidate.Candidate;

namespace ERMS.UnitTests.Features.JobPostings.Queries.GetMySavedPosts;

public class GetMySavedPostsHandlerTests
{
    private readonly Mock<IERMSDbContext> _contextMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<ILogger<GetMySavedPostsHandler>> _loggerMock;
    private readonly GetMySavedPostsHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _candidateId = Guid.NewGuid();
    private readonly Guid _enterpriseId = Guid.NewGuid();

    public GetMySavedPostsHandlerTests()
    {
        _contextMock = new Mock<IERMSDbContext>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _loggerMock = new Mock<ILogger<GetMySavedPostsHandler>>();

        _handler = new GetMySavedPostsHandler(
            _contextMock.Object,
            _currentUserServiceMock.Object,
            _loggerMock.Object);
    }

    #region Helpers

    private void SetupAuthenticatedCandidate()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(x => x.Roles).Returns([AppRoles.Candidate]);
    }

    private CandidateEntity CreateCandidate() => new()
    {
        Id = _candidateId,
        UserId = _userId,
        IsDeleted = false,
        User = new User { Id = _userId, FullName = "Test", Email = "t@t.com" }
    };

    private Enterprise CreateEnterprise() => new()
    {
        Id = _enterpriseId,
        EnterpriseName = "ACME Corp",
        EnterpriseCode = "ACME",
        IsDeleted = false
    };

    private JobPosting CreateJobPosting(string title = "Dev", string? location = "HN") => new()
    {
        Id = Guid.NewGuid(),
        EnterpriseId = _enterpriseId,
        Enterprise = CreateEnterprise(),
        JobTitle = title,
        JobCode = "JP-001",
        Location = location,
        EmploymentType = "FullTime",
        Description = "Desc",
        Status = "Published",
        IsDeleted = false
    };

    private SavedJob CreateSavedJob(JobPosting? jp = null, DateTime? savedAt = null, Guid? candidateId = null) => new()
    {
        Id = Guid.NewGuid(),
        CandidateId = candidateId ?? _candidateId,
        JobPostingId = (jp ?? CreateJobPosting()).Id,
        JobPosting = jp ?? CreateJobPosting(),
        SavedAt = savedAt ?? DateTime.UtcNow
    };

    private void SetupCandidatesDbSet(CandidateEntity? c = null)
    {
        var data = c != null ? new List<CandidateEntity> { c } : new List<CandidateEntity>();
        _contextMock.Setup(x => x.Candidates).Returns(data.AsQueryable().BuildMockDbSet().Object);
    }

    private void SetupSavedJobsDbSet(List<SavedJob>? items = null)
    {
        var data = items ?? [];
        _contextMock.Setup(x => x.SavedJobs).Returns(data.AsQueryable().BuildMockDbSet().Object);
    }

    #endregion

    #region Auth & Role Tests

    [Fact]
    public async Task Handle_ShouldThrow_WhenNotAuthenticated()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);
        await _handler.Invoking(h => h.Handle(new GetMySavedPostsQuery(), CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Người dùng chưa được xác thực.");
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenNotCandidate()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(x => x.Roles).Returns([AppRoles.HRManager]);
        await _handler.Invoking(h => h.Handle(new GetMySavedPostsQuery(), CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Chỉ ứng viên mới có quyền xem bài viết đã lưu.");
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenCandidateNotFound()
    {
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(null);
        await _handler.Invoking(h => h.Handle(new GetMySavedPostsQuery(), CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("Không tìm thấy hồ sơ ứng viên.");
    }

    #endregion

    #region Happy Paths

    [Fact]
    public async Task Handle_ShouldReturnEmpty_WhenNoSavedPosts()
    {
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidate());
        SetupSavedJobsDbSet([]);

        var result = await _handler.Handle(new GetMySavedPostsQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldReturnSavedPosts_WithJobDetails()
    {
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidate());

        var jp = CreateJobPosting("Senior Dev", "HCMC");
        var saved = CreateSavedJob(jp);
        SetupSavedJobsDbSet([saved]);

        var result = await _handler.Handle(new GetMySavedPostsQuery(), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        var item = result.Items[0];
        item.JobTitle.Should().Be("Senior Dev");
        item.Location.Should().Be("HCMC");
        item.CompanyName.Should().Be("ACME Corp");
    }

    [Fact]
    public async Task Handle_ShouldExcludeOtherCandidatesPosts()
    {
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidate());

        var own = CreateSavedJob(candidateId: _candidateId);
        var other = CreateSavedJob(candidateId: Guid.NewGuid());
        SetupSavedJobsDbSet([own, other]);

        var result = await _handler.Handle(new GetMySavedPostsQuery(), CancellationToken.None);
        result.Items.Should().HaveCount(1);
    }

    #endregion

    #region Sorting & Pagination

    [Fact]
    public async Task Handle_ShouldSortBySavedAtDescending()
    {
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidate());

        var older = CreateSavedJob(savedAt: DateTime.UtcNow.AddDays(-5));
        var newer = CreateSavedJob(savedAt: DateTime.UtcNow.AddDays(-1));
        SetupSavedJobsDbSet([older, newer]);

        var result = await _handler.Handle(new GetMySavedPostsQuery(), CancellationToken.None);

        result.Items[0].SavedAt.Should().BeAfter(result.Items[1].SavedAt);
    }

    [Fact]
    public async Task Handle_ShouldPaginateCorrectly()
    {
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidate());

        var items = Enumerable.Range(0, 15)
            .Select(i => CreateSavedJob(savedAt: DateTime.UtcNow.AddHours(-i)))
            .ToList();
        SetupSavedJobsDbSet(items);

        var result = await _handler.Handle(
            new GetMySavedPostsQuery { PageNumber = 2, PageSize = 10 }, CancellationToken.None);

        result.TotalCount.Should().Be(15);
        result.PageNumber.Should().Be(2);
        result.Items.Should().HaveCount(5);
    }

    #endregion
}
