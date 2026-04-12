using ERMS.Application.Features.Applications.Queries.GetApplicationResumeDownload;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Candidate;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Recruitment;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using System.Linq.Expressions;

using ApplicationEntity = ERMS.Domain.Entities.Application.Application;

namespace ERMS.UnitTests.Features.Applications.Queries.GetApplicationResumeDownload;

public class GetApplicationResumeDownloadHandlerTests
{
    private readonly Mock<IERMSDbContext> _contextMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<ICloudinaryService> _cloudinaryServiceMock;
    private readonly GetApplicationResumeDownloadHandler _handler;

    private readonly Guid _applicationId = Guid.NewGuid();
    private readonly Guid _candidateUserId = Guid.NewGuid();
    private readonly Guid _enterpriseId = Guid.NewGuid();

    public GetApplicationResumeDownloadHandlerTests()
    {
        _contextMock = new Mock<IERMSDbContext>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _cloudinaryServiceMock = new Mock<ICloudinaryService>();

        _handler = new GetApplicationResumeDownloadHandler(
            _contextMock.Object,
            _currentUserServiceMock.Object,
            _cloudinaryServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnSignedDownloadUrl_WhenHrManagerOwnsEnterprise()
    {
        // Arrange
        const string storedResumeUrl = "https://res.cloudinary.com/demo-cloud/raw/upload/v1775814433/erms/resumes/1775814434_Hieu_Lul_TopCV.vn_240326.135248.pdf";
        const string signedDownloadUrl = "https://api.cloudinary.com/v1_1/demo-cloud/raw/download?signature=test";

        _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
        _currentUserServiceMock.Setup(x => x.Roles).Returns([AppRoles.HRManager]);
        _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(_enterpriseId);

        SetupApplicationsDbSet(CreateApplication(storedResumeUrl, departmentId: 7));

        _cloudinaryServiceMock
            .Setup(x => x.GetAuthenticatedDownloadUrl(
                storedResumeUrl,
                "resume.pdf",
                It.IsAny<TimeSpan?>()))
            .Returns(signedDownloadUrl);

        // Act
        var result = await _handler.Handle(
            new GetApplicationResumeDownloadQuery { ApplicationId = _applicationId },
            CancellationToken.None);

        // Assert
        result.DownloadUrl.Should().Be(signedDownloadUrl);

        _cloudinaryServiceMock.Verify(x => x.GetAuthenticatedDownloadUrl(
            storedResumeUrl,
            "resume.pdf",
            It.IsAny<TimeSpan?>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenDepartmentHeadFromDifferentDepartment()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
        _currentUserServiceMock.Setup(x => x.Roles).Returns([AppRoles.DepartmentHead]);
        _currentUserServiceMock.Setup(x => x.GetDepartmentIdAsync()).ReturnsAsync(99);

        SetupApplicationsDbSet(CreateApplication("https://res.cloudinary.com/demo-cloud/raw/upload/v1/erms/resumes/resume.pdf", departmentId: 7));

        // Act
        var act = () => _handler.Handle(
            new GetApplicationResumeDownloadQuery { ApplicationId = _applicationId },
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*phòng ban*");
    }

    private ApplicationEntity CreateApplication(string resumeUrl, int departmentId)
    {
        var candidateUser = new User
        {
            Id = _candidateUserId,
            FullName = "Candidate One",
            Email = "candidate@test.com"
        };

        var candidate = new Domain.Entities.Candidate.Candidate
        {
            Id = Guid.NewGuid(),
            UserId = _candidateUserId,
            User = candidateUser,
            IsDeleted = false
        };

        var resume = new Resume
        {
            Id = Guid.NewGuid(),
            CandidateId = candidate.Id,
            FileName = "resume.pdf",
            FileUrl = resumeUrl,
            IsDeleted = false
        };

        var jobPosting = new JobPosting
        {
            Id = Guid.NewGuid(),
            EnterpriseId = _enterpriseId,
            DepartmentId = departmentId,
            JobTitle = "Backend Engineer",
            Description = "Build APIs",
            CreatedById = Guid.NewGuid(),
            IsDeleted = false
        };

        return new ApplicationEntity
        {
            Id = _applicationId,
            CandidateId = candidate.Id,
            Candidate = candidate,
            ResumeId = resume.Id,
            Resume = resume,
            JobPostingId = jobPosting.Id,
            JobPosting = jobPosting,
            Stage = "Applied",
            Status = "Active",
            AppliedAt = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    private void SetupApplicationsDbSet(params ApplicationEntity[] applications)
    {
        var data = applications.AsQueryable();
        var mockSet = CreateMockDbSet(data);
        _contextMock.Setup(c => c.Applications).Returns(mockSet.Object);
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
}

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
        return _inner.Execute(expression)!;
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
                types: [typeof(Expression)])!
            .MakeGenericMethod(expectedResultType)
            .Invoke(this, [expression]);

        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(expectedResultType)
            .Invoke(null, [executionResult])!;
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

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
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

    public T Current => _inner.Current;
}
