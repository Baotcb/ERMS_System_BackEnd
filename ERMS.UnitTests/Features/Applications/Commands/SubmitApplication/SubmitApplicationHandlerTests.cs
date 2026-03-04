using ERMS.Application.Features.Applications.Commands.SubmitApplication;
using ERMS.Application.Interface;
using FluentAssertions;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Constants.Recruitment;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Candidate;
using ERMS.Domain.Entities.Recruitment;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using System.Text;

// Alias to avoid namespace collision
using ApplicationEntity = ERMS.Domain.Entities.Application.Application;
using CandidateEntity = ERMS.Domain.Entities.Candidate.Candidate;

namespace ERMS.UnitTests.Features.Applications.Commands.SubmitApplication;

public class SubmitApplicationHandlerTests
{
    private readonly Mock<IERMSDbContext> _contextMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<ICloudinaryService> _cloudinaryServiceMock;
    private readonly Mock<IPdfTextExtractor> _pdfTextExtractorMock;
    private readonly Mock<IBackgroundTaskQueue> _backgroundQueueMock;
    private readonly Mock<ILogger<SubmitApplicationHandler>> _loggerMock;
    private readonly SubmitApplicationHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _candidateId = Guid.NewGuid();
    private readonly Guid _jobPostingId = Guid.NewGuid();

    public SubmitApplicationHandlerTests()
    {
        _contextMock = new Mock<IERMSDbContext>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _cloudinaryServiceMock = new Mock<ICloudinaryService>();
        _pdfTextExtractorMock = new Mock<IPdfTextExtractor>();
        _backgroundQueueMock = new Mock<IBackgroundTaskQueue>();
        _loggerMock = new Mock<ILogger<SubmitApplicationHandler>>();

        // Setup successful mocks by default for services to avoid null reference in happy paths
        // Specific tests can override these
        _cloudinaryServiceMock
            .Setup(x => x.UploadPdfAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(("https://cloudinary.com/resume.pdf", "erms/resumes/123"));
            
        _pdfTextExtractorMock
            .Setup(x => x.ExtractTextAsync(It.IsAny<Stream>()))
            .ReturnsAsync("John Doe\nSoftware Engineer\n5 years experience in C# and .NET");

        _handler = new SubmitApplicationHandler(
            _contextMock.Object,
            _currentUserServiceMock.Object,
            _cloudinaryServiceMock.Object,
            _pdfTextExtractorMock.Object,
            _backgroundQueueMock.Object,
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

    private CandidateEntity CreateCandidate()
    {
        return new CandidateEntity
        {
            Id = _candidateId,
            UserId = _userId,
            Headline = "Software Engineer",
            CurrentPosition = "Senior Developer",
            IsDeleted = false
        };
    }

    private JobPosting CreatePublishedJobPosting()
    {
        return new JobPosting
        {
            Id = _jobPostingId,
            JobTitle = "Software Engineer",
            Description = "Looking for a .NET developer",
            Requirements = "[\"C#\", \".NET\", \"SQL\"]",
            Status = JobPostingStatus.Published,
            ApplicationDeadline = DateTime.UtcNow.AddMonths(1),
            IsDeleted = false
        };
    }

    private Mock<IFormFile> CreateMockPdfFile()
    {
        var mockFile = new Mock<IFormFile>();
        var content = "PDF content placeholder";
        var fileName = "resume.pdf";
        var bytes = Encoding.UTF8.GetBytes(content);

        // FIX: Use lambda to return a NEW Stream instance each time OpenReadStream is called
        // This prevents ObjectDisposedException when the handler reads the stream multiple times
        mockFile.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(bytes));
        
        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.Length).Returns(bytes.Length);
        mockFile.Setup(f => f.ContentType).Returns("application/pdf");

        return mockFile;
    }

    private SubmitApplicationCommand CreateValidCommand()
    {
        return new SubmitApplicationCommand
        {
            JobPostingId = _jobPostingId,
            CvFile = CreateMockPdfFile().Object,
            CoverLetter = "I am interested in this position.",
            ExpectedSalary = 50000
        };
    }



    private void SetupFullMocksForSuccess()
    {
        var candidate = CreateCandidate();
        var jobPosting = CreatePublishedJobPosting();

        // Candidates DbSet
        var candidatesData = new List<CandidateEntity> { candidate }.AsQueryable();
        var candidatesMockSet = CreateMockDbSet(candidatesData);
        _contextMock.Setup(c => c.Candidates).Returns(candidatesMockSet.Object);

        // JobPostings DbSet
        var jobPostingsData = new List<JobPosting> { jobPosting }.AsQueryable();
        var jobPostingsMockSet = CreateMockDbSet(jobPostingsData);
        _contextMock.Setup(c => c.JobPostings).Returns(jobPostingsMockSet.Object);

        // Applications DbSet (empty - no existing applications)
        var applicationsData = new List<ApplicationEntity>().AsQueryable();
        var applicationsMockSet = CreateMockDbSet(applicationsData);
        _contextMock.Setup(c => c.Applications).Returns(applicationsMockSet.Object);

        // Resumes DbSet
        var resumesData = new List<Resume>().AsQueryable();
        var resumesMockSet = CreateMockDbSet(resumesData);
        _contextMock.Setup(c => c.Resumes).Returns(resumesMockSet.Object);

        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    #endregion

    #region Security Tests

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
        _currentUserServiceMock.Setup(x => x.Roles).Returns([AppRoles.HRManager]); // Wrong role

        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Only candidates can submit job applications.");
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
            .WithMessage("Only candidates can submit job applications.");
    }

    #endregion

    #region Candidate Profile Tests

    [Fact]
    public async Task Handle_ShouldThrowException_WhenCandidateProfileNotFound()
    {
        // Arrange
        SetupAuthenticatedCandidate();

        var candidatesData = new List<CandidateEntity>().AsQueryable(); // Empty
        var candidatesMockSet = CreateMockDbSet(candidatesData);
        _contextMock.Setup(c => c.Candidates).Returns(candidatesMockSet.Object);

        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("*Candidate profile not found*");
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenCandidateProfileIsDeleted()
    {
        // Arrange
        SetupAuthenticatedCandidate();

        var candidate = CreateCandidate();
        candidate.IsDeleted = true; // Soft deleted

        var candidatesData = new List<CandidateEntity> { candidate }.AsQueryable();
        var candidatesMockSet = CreateMockDbSet(candidatesData);
        _contextMock.Setup(c => c.Candidates).Returns(candidatesMockSet.Object);

        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("*Candidate profile not found*");
    }

    #endregion

    #region Job Posting Validation Tests

    [Fact]
    public async Task Handle_ShouldThrowException_WhenJobPostingNotFound()
    {
        // Arrange
        SetupAuthenticatedCandidate();

        var candidate = CreateCandidate();
        var candidatesData = new List<CandidateEntity> { candidate }.AsQueryable();
        var candidatesMockSet = CreateMockDbSet(candidatesData);
        _contextMock.Setup(c => c.Candidates).Returns(candidatesMockSet.Object);

        var jobPostingsData = new List<JobPosting>().AsQueryable(); // Empty
        var jobPostingsMockSet = CreateMockDbSet(jobPostingsData);
        _contextMock.Setup(c => c.JobPostings).Returns(jobPostingsMockSet.Object);

        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("*Job posting with ID*not found*");
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenJobPostingIsNotPublished()
    {
        // Arrange
        SetupAuthenticatedCandidate();

        var candidate = CreateCandidate();
        var candidatesData = new List<CandidateEntity> { candidate }.AsQueryable();
        var candidatesMockSet = CreateMockDbSet(candidatesData);
        _contextMock.Setup(c => c.Candidates).Returns(candidatesMockSet.Object);

        var jobPosting = CreatePublishedJobPosting();
        jobPosting.Status = JobPostingStatus.Draft; // Not published

        var jobPostingsData = new List<JobPosting> { jobPosting }.AsQueryable();
        var jobPostingsMockSet = CreateMockDbSet(jobPostingsData);
        _contextMock.Setup(c => c.JobPostings).Returns(jobPostingsMockSet.Object);

        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("*not open for applications*");
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenJobPostingIsClosed()
    {
        // Arrange
        SetupAuthenticatedCandidate();

        var candidate = CreateCandidate();
        var candidatesData = new List<CandidateEntity> { candidate }.AsQueryable();
        var candidatesMockSet = CreateMockDbSet(candidatesData);
        _contextMock.Setup(c => c.Candidates).Returns(candidatesMockSet.Object);

        var jobPosting = CreatePublishedJobPosting();
        jobPosting.Status = JobPostingStatus.Closed;

        var jobPostingsData = new List<JobPosting> { jobPosting }.AsQueryable();
        var jobPostingsMockSet = CreateMockDbSet(jobPostingsData);
        _contextMock.Setup(c => c.JobPostings).Returns(jobPostingsMockSet.Object);

        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("*not open for applications*");
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenApplicationDeadlineHasPassed()
    {
        // Arrange
        SetupAuthenticatedCandidate();

        var candidate = CreateCandidate();
        var candidatesData = new List<CandidateEntity> { candidate }.AsQueryable();
        var candidatesMockSet = CreateMockDbSet(candidatesData);
        _contextMock.Setup(c => c.Candidates).Returns(candidatesMockSet.Object);

        var jobPosting = CreatePublishedJobPosting();
        jobPosting.ApplicationDeadline = DateTime.UtcNow.AddDays(-1); // Past deadline

        var jobPostingsData = new List<JobPosting> { jobPosting }.AsQueryable();
        var jobPostingsMockSet = CreateMockDbSet(jobPostingsData);
        _contextMock.Setup(c => c.JobPostings).Returns(jobPostingsMockSet.Object);

        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("*deadline has passed*");
    }

    #endregion

    #region Duplicate Application Tests

    [Fact]
    public async Task Handle_ShouldThrowException_WhenCandidateAlreadyApplied()
    {
        // Arrange
        SetupAuthenticatedCandidate();

        var candidate = CreateCandidate();
        var candidatesData = new List<CandidateEntity> { candidate }.AsQueryable();
        var candidatesMockSet = CreateMockDbSet(candidatesData);
        _contextMock.Setup(c => c.Candidates).Returns(candidatesMockSet.Object);

        var jobPosting = CreatePublishedJobPosting();
        var jobPostingsData = new List<JobPosting> { jobPosting }.AsQueryable();
        var jobPostingsMockSet = CreateMockDbSet(jobPostingsData);
        _contextMock.Setup(c => c.JobPostings).Returns(jobPostingsMockSet.Object);

        // Existing application
        var existingApplication = new ApplicationEntity
        {
            Id = Guid.NewGuid(),
            JobPostingId = _jobPostingId,
            CandidateId = _candidateId,
            IsDeleted = false
        };
        var applicationsData = new List<ApplicationEntity> { existingApplication }.AsQueryable();
        var applicationsMockSet = CreateMockDbSet(applicationsData);
        _contextMock.Setup(c => c.Applications).Returns(applicationsMockSet.Object);

        var command = CreateValidCommand();

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("*already applied*");
    }

    [Fact]
    public async Task Handle_ShouldAllowApplication_WhenPreviousWasDeleted()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupFullMocksForSuccess();

        // Add a deleted previous application to checking DbContext
        var deletedApplication = new ApplicationEntity
        {
            Id = Guid.NewGuid(),
            JobPostingId = _jobPostingId,
            CandidateId = _candidateId,
            IsDeleted = true // Soft deleted
        };

        var applicationsData = new List<ApplicationEntity> { deletedApplication }.AsQueryable();
        var applicationsMockSet = CreateMockDbSet(applicationsData);
        _contextMock.Setup(c => c.Applications).Returns(applicationsMockSet.Object);

        var command = CreateValidCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - Should succeed because previous application was deleted
        result.ApplicationId.Should().NotBe(Guid.Empty);
    }

    #endregion

    #region Background Queue Tests

    [Fact]
    public async Task Handle_ShouldEnqueueBackgroundTask_AfterSavingApplication()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupFullMocksForSuccess();

        var command = CreateValidCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - Background queue should receive the work item with correct data
        _backgroundQueueMock.Verify(
            x => x.EnqueueAsync(
                It.Is<CvScoringWorkItem>(item =>
                    item.ApplicationId == result.ApplicationId &&
                    item.ResumeText.Contains("John Doe") &&
                    item.JobDescription.Contains(".NET developer")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnNullCVScreeningResult()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupFullMocksForSuccess();

        var command = CreateValidCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - CVScreeningResult should be null (scored asynchronously)
        result.CVScreeningResult.Should().BeNull();
    }

    #endregion

    #region Successful Submission Tests

    [Fact]
    public async Task Handle_ShouldReturnCompleteResult_OnSuccessfulSubmission()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupFullMocksForSuccess();

        var command = CreateValidCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ApplicationId.Should().NotBe(Guid.Empty);
        result.ResumeId.Should().NotBe(Guid.Empty);
        result.ResumeUrl.Should().Be("https://cloudinary.com/resume.pdf");
        result.Stage.Should().Be(ApplicationStage.Applied);
        result.CVScreeningResult.Should().BeNull(); // CV scoring is now async
    }

    [Fact]
    public async Task Handle_ShouldCallCloudinaryUploadOnce()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupFullMocksForSuccess();

        var command = CreateValidCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _cloudinaryServiceMock.Verify(
            x => x.UploadPdfAsync(It.IsAny<Stream>(), It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCallPdfExtractorOnce()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupFullMocksForSuccess();

        var command = CreateValidCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _pdfTextExtractorMock.Verify(
            x => x.ExtractTextAsync(It.IsAny<Stream>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldEnqueueCvScoringWithCorrectParameters()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupFullMocksForSuccess();

        var command = CreateValidCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - Verify the background queue received correct work item data
        _backgroundQueueMock.Verify(
            x => x.EnqueueAsync(
                It.Is<CvScoringWorkItem>(item =>
                    item.ResumeText.Contains("John Doe") &&
                    item.JobDescription.Contains(".NET developer") &&
                    item.RequiredSkills.Contains("C#")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldSaveChangesToDatabase()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupFullMocksForSuccess();

        var command = CreateValidCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}

#region Test Infrastructure



// Helper classes for Async Query Provider (Reused locally to avoid dependencies)
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
            return _inner.Execute(expression);
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
                    types: new[] { typeof(Expression) })
                .MakeGenericMethod(expectedResultType)
                .Invoke(this, new[] { expression });

            return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))
                .MakeGenericMethod(expectedResultType)
                .Invoke(null, new[] { executionResult });
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

        IQueryProvider IQueryable.Provider
        {
            get { return new TestAsyncQueryProvider<T>(this); }
        }
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

        public T Current
        {
            get { return _inner.Current; }
        }
    }

#endregion
