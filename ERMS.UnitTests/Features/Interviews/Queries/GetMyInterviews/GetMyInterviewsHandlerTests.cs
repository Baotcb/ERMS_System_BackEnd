using ERMS.Application.Features.Interviews.Queries.GetMyInterviews;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Entities.Application;
using ERMS.Domain.Entities.Candidate;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Organization;
using ERMS.Domain.Entities.Recruitment;
using ERMS.Domain.Enums;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Moq;
using ApplicationEntity = ERMS.Domain.Entities.Application.Application;
using Xunit;

namespace ERMS.UnitTests.Features.Interviews.Queries.GetMyInterviews;

public class GetMyInterviewsHandlerTests
{
    private readonly Mock<IERMSDbContext> _mockContext;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly GetMyInterviewsHandler _handler;

    // Shared test data
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _employeeId = Guid.NewGuid();
    private readonly Guid _enterpriseId = Guid.NewGuid();
    private readonly Guid _interviewId = Guid.NewGuid();
    private readonly Guid _applicationId = Guid.NewGuid();
    private readonly Guid _participantId = Guid.NewGuid();

    public GetMyInterviewsHandlerTests()
    {
        _mockContext = new Mock<IERMSDbContext>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _handler = new GetMyInterviewsHandler(_mockContext.Object, _mockCurrentUserService.Object);
    }

    private GetMyInterviewsQuery CreateValidQuery() => new()
    {
        PageNumber = 1,
        PageSize = 20,
        StatusFilter = null
    };

    private void SetupAuthenticatedUser()
    {
        _mockCurrentUserService.Setup(s => s.UserId).Returns(_userId);
        _mockCurrentUserService.Setup(s => s.GetEnterpriseIdAsync()).ReturnsAsync(_enterpriseId);
    }

    private Employee CreateEmployee() => new()
    {
        Id = _employeeId,
        UserId = _userId,
        IsDeleted = false,
        User = new User { FullName = "Test Interviewer" }
    };

    private InterviewParticipant CreateParticipant(
        Interview? interview = null,
        bool feedbackSubmitted = false)
    {
        var defaultInterview = interview ?? CreateInterview();
        return new InterviewParticipant
        {
            Id = _participantId,
            InterviewId = defaultInterview.Id,
            EmployeeId = _employeeId,
            Role = "Interviewer",
            IsRequired = true,
            ConfirmationStatus = "Confirmed",
            FeedbackSubmittedAt = feedbackSubmitted ? DateTime.UtcNow : null,
            Interview = defaultInterview
        };
    }

    private Interview CreateInterview(
        string status = InterviewStatus.Scheduled,
        bool isDeleted = false)
    {
        var jobPosting = new JobPosting
        {
            Id = Guid.NewGuid(),
            EnterpriseId = _enterpriseId,
            JobTitle = "Senior Developer",
            IsDeleted = false
        };
        var candidate = new Candidate
        {
            Id = Guid.NewGuid(),
            User = new User { FullName = "John Doe", Email = "john@example.com" }
        };
        var application = new ApplicationEntity
        {
            Id = _applicationId,
            Stage = ApplicationStage.InterviewScheduled,
            JobPosting = jobPosting,
            Candidate = candidate,
            IsDeleted = false
        };
        return new Interview
        {
            Id = _interviewId,
            ApplicationId = _applicationId,
            Status = status,
            InterviewType = "Technical",
            InterviewFormat = InterviewFormat.Online,
            RoundNumber = 1,
            ScheduledAt = DateTime.UtcNow.AddDays(3),
            Duration = 60,
            Location = null,
            MeetingLink = "https://meet.google.com/test",
            ScheduledById = Guid.NewGuid(),
            IsDeleted = isDeleted,
            Application = application
        };
    }

    [Fact]
    public async Task Handle_ShouldReturnPaginatedInterviews_WhenValid()
    {
        // Arrange
        SetupAuthenticatedUser();
        var employee = CreateEmployee();
        var interview = CreateInterview();
        var participant = CreateParticipant(interview);

        var employees = new List<Employee> { employee }.AsQueryable().BuildMockDbSet();
        var participants = new List<InterviewParticipant> { participant }.AsQueryable().BuildMockDbSet();

        _mockContext.Setup(c => c.Employees).Returns(employees.Object);
        _mockContext.Setup(c => c.InterviewParticipants).Returns(participants.Object);

        var query = CreateValidQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(1);
        result.PageNumber.Should().Be(1);
        result.Items.Should().HaveCount(1);

        var item = result.Items[0];
        item.InterviewId.Should().Be(_interviewId);
        item.CandidateName.Should().Be("John Doe");
        item.CandidateEmail.Should().Be("john@example.com");
        item.JobTitle.Should().Be("Senior Developer");
        item.InterviewType.Should().Be("Technical");
        item.InterviewFormat.Should().Be("Online");
        item.RoundNumber.Should().Be(1);
        item.Duration.Should().Be(60);
        item.MeetingLink.Should().Be("https://meet.google.com/test");
        item.Status.Should().Be(InterviewStatus.Scheduled);
        item.MyRole.Should().Be("Interviewer");
        item.MyConfirmationStatus.Should().Be("Confirmed");
        item.HasSubmittedFeedback.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange
        _mockCurrentUserService.Setup(s => s.UserId).Returns((Guid?)null);
        var query = CreateValidQuery();

        // Act
        var act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Người dùng chưa được xác thực.");
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorized_WhenNoEnterprise()
    {
        // Arrange
        _mockCurrentUserService.Setup(s => s.UserId).Returns(_userId);
        _mockCurrentUserService.Setup(s => s.GetEnterpriseIdAsync()).ReturnsAsync((Guid?)null);
        var query = CreateValidQuery();

        // Act
        var act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Người dùng không thuộc doanh nghiệp nào.");
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorized_WhenUserNotEmployee()
    {
        // Arrange
        SetupAuthenticatedUser();
        var employees = new List<Employee>().AsQueryable().BuildMockDbSet();
        _mockContext.Setup(c => c.Employees).Returns(employees.Object);

        var query = CreateValidQuery();

        // Act
        var act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Người dùng không phải là nhân viên.");
    }

    [Fact]
    public async Task Handle_ShouldFilterByStatus_WhenStatusFilterProvided()
    {
        // Arrange
        SetupAuthenticatedUser();
        var employee = CreateEmployee();

        var scheduledInterview = CreateInterview(InterviewStatus.Scheduled);
        var completedInterview = CreateInterview(InterviewStatus.Completed);
        completedInterview.Id = Guid.NewGuid();

        var participant1 = CreateParticipant(scheduledInterview);
        var participant2 = new InterviewParticipant
        {
            Id = Guid.NewGuid(),
            InterviewId = completedInterview.Id,
            EmployeeId = _employeeId,
            Role = "Interviewer",
            ConfirmationStatus = "Confirmed",
            Interview = completedInterview
        };

        var employees = new List<Employee> { employee }.AsQueryable().BuildMockDbSet();
        var participants = new List<InterviewParticipant> { participant1, participant2 }.AsQueryable().BuildMockDbSet();

        _mockContext.Setup(c => c.Employees).Returns(employees.Object);
        _mockContext.Setup(c => c.InterviewParticipants).Returns(participants.Object);

        var query = new GetMyInterviewsQuery
        {
            PageNumber = 1,
            PageSize = 20,
            StatusFilter = InterviewStatus.Scheduled
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items[0].Status.Should().Be(InterviewStatus.Scheduled);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmpty_WhenNoInterviews()
    {
        // Arrange
        SetupAuthenticatedUser();
        var employee = CreateEmployee();

        var employees = new List<Employee> { employee }.AsQueryable().BuildMockDbSet();
        var participants = new List<InterviewParticipant>().AsQueryable().BuildMockDbSet();

        _mockContext.Setup(c => c.Employees).Returns(employees.Object);
        _mockContext.Setup(c => c.InterviewParticipants).Returns(participants.Object);

        var query = CreateValidQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldExcludeDeleted_WhenInterviewIsDeleted()
    {
        // Arrange
        SetupAuthenticatedUser();
        var employee = CreateEmployee();
        var deletedInterview = CreateInterview(isDeleted: true);
        var participant = CreateParticipant(deletedInterview);

        var employees = new List<Employee> { employee }.AsQueryable().BuildMockDbSet();
        var participants = new List<InterviewParticipant> { participant }.AsQueryable().BuildMockDbSet();

        _mockContext.Setup(c => c.Employees).Returns(employees.Object);
        _mockContext.Setup(c => c.InterviewParticipants).Returns(participants.Object);

        var query = CreateValidQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }
}
