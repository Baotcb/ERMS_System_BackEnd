using ERMS.Application.Features.JobPostings.Commands.UnsaveJobPosting;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Candidate;
using ERMS.Domain.Entities.Identity;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

using CandidateEntity = ERMS.Domain.Entities.Candidate.Candidate;

namespace ERMS.UnitTests.Features.JobPostings.Commands.UnsaveJobPosting;

public class UnsaveJobPostingHandlerTests
{
    private readonly Mock<IERMSDbContext> _contextMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<ILogger<UnsaveJobPostingHandler>> _loggerMock;
    private readonly UnsaveJobPostingHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _candidateId = Guid.NewGuid();
    private readonly Guid _jobPostingId = Guid.NewGuid();

    public UnsaveJobPostingHandlerTests()
    {
        _contextMock = new Mock<IERMSDbContext>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _loggerMock = new Mock<ILogger<UnsaveJobPostingHandler>>();

        _handler = new UnsaveJobPostingHandler(
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

    private void SetupCandidatesDbSet(CandidateEntity? c = null)
    {
        var data = c != null ? new List<CandidateEntity> { c } : new List<CandidateEntity>();
        _contextMock.Setup(x => x.Candidates).Returns(data.AsQueryable().BuildMockDbSet().Object);
    }

    private void SetupSavedJobsDbSet(List<SavedJob>? items = null)
    {
        var data = items ?? [];
        var mockSet = data.AsQueryable().BuildMockDbSet();
        mockSet.Setup(m => m.Remove(It.IsAny<SavedJob>())).Callback<SavedJob>(s => data.Remove(s));
        _contextMock.Setup(x => x.SavedJobs).Returns(mockSet.Object);
    }

    #endregion

    #region Auth & Role Tests

    [Fact]
    public async Task Handle_ShouldThrow_WhenNotAuthenticated()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);
        var cmd = new UnsaveJobPostingCommand { JobPostingId = _jobPostingId };
        await _handler.Invoking(h => h.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("User not authenticated.");
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenNotCandidate()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(x => x.Roles).Returns([AppRoles.HRManager]);
        var cmd = new UnsaveJobPostingCommand { JobPostingId = _jobPostingId };
        await _handler.Invoking(h => h.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Only candidates can unsave job postings.");
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenCandidateNotFound()
    {
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(null);
        var cmd = new UnsaveJobPostingCommand { JobPostingId = _jobPostingId };
        await _handler.Invoking(h => h.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("Candidate profile not found.");
    }

    #endregion

    #region Business Logic Tests

    [Fact]
    public async Task Handle_ShouldThrow_WhenSavedJobNotFound()
    {
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidate());
        SetupSavedJobsDbSet([]);

        var cmd = new UnsaveJobPostingCommand { JobPostingId = _jobPostingId };
        await _handler.Invoking(h => h.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("Saved job not found.");
    }

    [Fact]
    public async Task Handle_ShouldSucceed_WhenSavedJobExists()
    {
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidate());

        var saved = new SavedJob
        {
            Id = Guid.NewGuid(),
            CandidateId = _candidateId,
            JobPostingId = _jobPostingId,
            SavedAt = DateTime.UtcNow
        };
        SetupSavedJobsDbSet([saved]);

        var cmd = new UnsaveJobPostingCommand { JobPostingId = _jobPostingId };
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.JobPostingId.Should().Be(_jobPostingId);
        result.Message.Should().Be("Job posting unsaved successfully.");
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldNotFindOtherCandidatesPost()
    {
        SetupAuthenticatedCandidate();
        SetupCandidatesDbSet(CreateCandidate());

        // Saved by a different candidate
        var otherSaved = new SavedJob
        {
            Id = Guid.NewGuid(),
            CandidateId = Guid.NewGuid(),
            JobPostingId = _jobPostingId,
            SavedAt = DateTime.UtcNow
        };
        SetupSavedJobsDbSet([otherSaved]);

        var cmd = new UnsaveJobPostingCommand { JobPostingId = _jobPostingId };
        await _handler.Invoking(h => h.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("Saved job not found.");
    }

    #endregion
}
