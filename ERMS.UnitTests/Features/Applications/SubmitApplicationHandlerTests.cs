using ERMS.Application.Features.Applications.Commands.SubmitApplication;
using ERMS.Application.Interface;
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

namespace ERMS.UnitTests.Features.Applications;

public class SubmitApplicationHandlerTests
{
    private readonly Mock<IERMSDbContext> _contextMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<ICloudinaryService> _cloudinaryServiceMock;
    private readonly Mock<IPdfTextExtractor> _pdfTextExtractorMock;
    private readonly Mock<IGeminiAIService> _geminiAIServiceMock;
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
        _geminiAIServiceMock = new Mock<IGeminiAIService>();
        _loggerMock = new Mock<ILogger<SubmitApplicationHandler>>();

        // Setup successful mocks by default for services to avoid null reference in happy paths
        // Specific tests can override these
        _cloudinaryServiceMock
            .Setup(x => x.UploadPdfAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(("https://cloudinary.com/resume.pdf", "erms/resumes/123"));
            
        _pdfTextExtractorMock
            .Setup(x => x.ExtractTextAsync(It.IsAny<Stream>()))
            .ReturnsAsync("John Doe\nSoftware Engineer\n5 years experience in C# and .NET");
            
        _geminiAIServiceMock
            .Setup(x => x.AnalyzeResumeAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(CreateSuccessfulAIResult());

        _handler = new SubmitApplicationHandler(
            _contextMock.Object,
            _currentUserServiceMock.Object,
            _cloudinaryServiceMock.Object,
            _pdfTextExtractorMock.Object,
            _geminiAIServiceMock.Object,
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

    private CVScreeningResultDto CreateSuccessfulAIResult()
    {
        return new CVScreeningResultDto
        {
            OverallScore = 85,
            SkillMatchScore = 90,
            ExperienceMatchScore = 80,
            EducationMatchScore = 75,
            KeywordMatchScore = 88,
            MatchedSkills = ["C#", ".NET"],
            MissingSkills = ["Azure"],
            Strengths = ["Strong programming background"],
            Concerns = [],
            Summary = "Good candidate fit.",
            RawResponse = "{}"
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

        // CVScreeningResults DbSet
        var screeningData = new List<ERMS.Domain.Entities.Application.CVScreeningResult>().AsQueryable();
        var screeningMockSet = CreateMockDbSet(screeningData);
        _contextMock.Setup(c => c.CVScreeningResults).Returns(screeningMockSet.Object);

        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    #endregion

    #region Security Tests

    [Fact]
    public async Task Should_Throw_UnauthorizedAccessException_When_User_Not_Authenticated()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        var command = CreateValidCommand();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(command, CancellationToken.None));

        Assert.Equal("User not authenticated.", exception.Message);
    }

    [Fact]
    public async Task Should_Throw_UnauthorizedAccessException_When_User_Is_Not_Candidate()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(x => x.Roles).Returns([AppRoles.HRManager]); // Wrong role

        var command = CreateValidCommand();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(command, CancellationToken.None));

        Assert.Equal("Only candidates can submit job applications.", exception.Message);
    }

    [Fact]
    public async Task Should_Throw_UnauthorizedAccessException_When_User_Has_No_Roles()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(x => x.Roles).Returns((List<string>?)null);

        var command = CreateValidCommand();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(command, CancellationToken.None));

        Assert.Equal("Only candidates can submit job applications.", exception.Message);
    }

    #endregion

    #region Candidate Profile Tests

    [Fact]
    public async Task Should_Throw_Exception_When_Candidate_Profile_Not_Found()
    {
        // Arrange
        SetupAuthenticatedCandidate();

        var candidatesData = new List<CandidateEntity>().AsQueryable(); // Empty
        var candidatesMockSet = CreateMockDbSet(candidatesData);
        _contextMock.Setup(c => c.Candidates).Returns(candidatesMockSet.Object);

        var command = CreateValidCommand();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _handler.Handle(command, CancellationToken.None));

        Assert.Contains("Candidate profile not found", exception.Message);
    }

    [Fact]
    public async Task Should_Throw_Exception_When_Candidate_Profile_Is_Deleted()
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
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _handler.Handle(command, CancellationToken.None));

        Assert.Contains("Candidate profile not found", exception.Message);
    }

    #endregion

    #region Job Posting Validation Tests

    [Fact]
    public async Task Should_Throw_Exception_When_JobPosting_Not_Found()
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
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _handler.Handle(command, CancellationToken.None));

        Assert.Contains("Job posting with ID", exception.Message);
        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task Should_Throw_Exception_When_JobPosting_Is_Not_Published()
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
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _handler.Handle(command, CancellationToken.None));

        Assert.Contains("not open for applications", exception.Message);
    }

    [Fact]
    public async Task Should_Throw_Exception_When_JobPosting_Is_Closed()
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
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _handler.Handle(command, CancellationToken.None));

        Assert.Contains("not open for applications", exception.Message);
    }

    [Fact]
    public async Task Should_Throw_Exception_When_Application_Deadline_Has_Passed()
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
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _handler.Handle(command, CancellationToken.None));

        Assert.Contains("deadline has passed", exception.Message);
    }

    #endregion

    #region Duplicate Application Tests

    [Fact]
    public async Task Should_Throw_Exception_When_Candidate_Already_Applied()
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
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _handler.Handle(command, CancellationToken.None));

        Assert.Contains("already applied", exception.Message);
    }

    [Fact]
    public async Task Should_Allow_Application_When_Previous_Was_Deleted()
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
        Assert.NotEqual(Guid.Empty, result.ApplicationId);
    }

    #endregion

    #region AI Failure Graceful Degradation Tests

    [Fact]
    public async Task Should_Submit_Application_With_Default_Scores_When_AI_Fails()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupFullMocksForSuccess();

        // Make AI service throw exception
        _geminiAIServiceMock
            .Setup(x => x.AnalyzeResumeAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>()))
            .ThrowsAsync(new Exception("Gemini API rate limit exceeded"));

        var command = CreateValidCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - Application should still be submitted with default scores
        Assert.NotEqual(Guid.Empty, result.ApplicationId);
        Assert.Equal(0, result.CVScreeningResult?.OverallScore);
        Assert.Contains("AI scoring unavailable", result.CVScreeningResult?.Summary ?? "");
    }

    #endregion

    #region Successful Submission Tests

    [Fact]
    public async Task Should_Return_Complete_Result_On_Successful_Submission()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupFullMocksForSuccess();

        var command = CreateValidCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result.ApplicationId);
        Assert.NotEqual(Guid.Empty, result.ResumeId);
        Assert.Equal("https://cloudinary.com/resume.pdf", result.ResumeUrl);
        Assert.Equal(ApplicationStage.Applied, result.Stage);
        Assert.NotNull(result.CVScreeningResult);
        Assert.Equal(85, result.CVScreeningResult.OverallScore);
    }

    [Fact]
    public async Task Should_Call_Cloudinary_Upload_Once()
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
    public async Task Should_Call_PDF_Extractor_Once()
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
    public async Task Should_Call_AI_Service_With_Correct_Parameters()
    {
        // Arrange
        SetupAuthenticatedCandidate();
        SetupFullMocksForSuccess();

        var command = CreateValidCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _geminiAIServiceMock.Verify(
            x => x.AnalyzeResumeAsync(
                It.Is<string>(s => s.Contains("John Doe")), // Resume text
                It.Is<string>(s => s.Contains(".NET developer")), // Job description
                It.IsAny<string>(), // Requirements
                It.IsAny<string?>(), // Education level
                It.IsAny<string?>()), // Experience level
            Times.Once);
    }

    [Fact]
    public async Task Should_Save_Changes_To_Database()
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

internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;

    public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> MoveNextAsync() => new(_inner.MoveNext());

    public T Current => _inner.Current;
}

internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    internal TestAsyncQueryProvider(IQueryProvider inner) => _inner = inner;

    public IQueryable CreateQuery(Expression expression) => new TestAsyncEnumerable<TEntity>(expression);

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
        new TestAsyncEnumerable<TElement>(expression);

    public object? Execute(Expression expression) => _inner.Execute(expression);

    public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        var expectedResultType = typeof(TResult).GetGenericArguments()[0];
        var executionResult = typeof(IQueryProvider)
            .GetMethod(nameof(IQueryProvider.Execute), 1, [typeof(Expression)])
            ?.MakeGenericMethod(expectedResultType)
            .Invoke(_inner, [expression]);

        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))
            ?.MakeGenericMethod(expectedResultType)
            .Invoke(null, [executionResult])!;
    }
}

internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }

    public TestAsyncEnumerable(Expression expression) : base(expression) { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
        new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

#endregion
