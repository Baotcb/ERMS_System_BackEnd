using ERMS.Application.Features.Applications.Commands.SubmitInterviewFeedback;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Entities.Application;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Organization;
using ERMS.Domain.Entities.Recruitment;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ApplicationEntity = ERMS.Domain.Entities.Application.Application;
using Xunit;

namespace ERMS.UnitTests.Features.Applications.Commands.SubmitInterviewFeedback;

public class SubmitInterviewFeedbackHandlerTests
{
    private readonly Mock<IERMSDbContext> _mockContext;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<ILogger<SubmitInterviewFeedbackHandler>> _mockLogger;
    private readonly SubmitInterviewFeedbackHandler _handler;

    // Shared test data
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _employeeId = Guid.NewGuid();
    private readonly Guid _applicationId = Guid.NewGuid();
    private readonly Guid _interviewId = Guid.NewGuid();
    private readonly Guid _participantId = Guid.NewGuid();
    private readonly Guid _enterpriseId = Guid.NewGuid();

    public SubmitInterviewFeedbackHandlerTests()
    {
        _mockContext = new Mock<IERMSDbContext>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockLogger = new Mock<ILogger<SubmitInterviewFeedbackHandler>>();
        _handler = new SubmitInterviewFeedbackHandler(_mockContext.Object, _mockCurrentUserService.Object, _mockLogger.Object);
    }

    private SubmitInterviewFeedbackCommand CreateValidCommand() => new()
    {
        ApplicationId = _applicationId,
        InterviewId = _interviewId,
        Rating = 4,
        Feedback = "Strong technical skills, good communication.",
        Recommendation = "Recommend for next round."
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
        User = new User { FullName = "Test Interviewer" }
    };

    private Interview CreateScheduledInterview(InterviewParticipant? participant = null)
    {
        var jobPosting = new JobPosting { Id = Guid.NewGuid(), EnterpriseId = _enterpriseId };
        var application = new ApplicationEntity { Id = _applicationId, Stage = ApplicationStage.InterviewScheduled, JobPosting = jobPosting };
        var interview = new Interview
        {
            Id = _interviewId,
            ApplicationId = _applicationId,
            Status = InterviewStatus.Scheduled,
            RoundNumber = 1,
            InterviewType = "Technical",
            ScheduledById = Guid.NewGuid(),
            Application = application
        };

        if (participant != null)
        {
            interview.Participants.Add(participant);
        }

        return interview;
    }

    private InterviewParticipant CreateParticipant(bool alreadySubmitted = false) => new()
    {
        Id = _participantId,
        InterviewId = _interviewId,
        EmployeeId = _employeeId,
        Role = "Interviewer",
        IsRequired = true,
        ConfirmationStatus = "Confirmed",
        FeedbackSubmittedAt = alreadySubmitted ? DateTime.UtcNow : null
    };

    [Fact]
    public async Task Handle_ShouldUpdateParticipantFeedback_WhenValid()
    {
        // Arrange
        SetupAuthenticatedUser();
        var employee = CreateEmployee();
        var participant = CreateParticipant();
        var interview = CreateScheduledInterview(participant);

        var employees = new List<Employee> { employee }.AsQueryable().BuildMockDbSet();
        var interviews = new List<Interview> { interview }.AsQueryable().BuildMockDbSet();

        _mockContext.Setup(c => c.Employees).Returns(employees.Object);
        _mockContext.Setup(c => c.Interviews).Returns(interviews.Object);

        var command = CreateValidCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ParticipantId.Should().Be(_participantId);
        result.InterviewId.Should().Be(_interviewId);
        result.Rating.Should().Be(4);
        result.FeedbackSubmittedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        participant.Rating.Should().Be(4);
        participant.Feedback.Should().Be("Strong technical skills, good communication.");
        participant.Recommendation.Should().Be("Recommend for next round.");
        participant.FeedbackSubmittedAt.Should().NotBeNull();

        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange
        _mockCurrentUserService.Setup(s => s.UserId).Returns((Guid?)null);
        var command = CreateValidCommand();

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Người dùng chưa được xác thực.");
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorized_WhenUserIsNotEmployee()
    {
        // Arrange
        SetupAuthenticatedUser();
        var employees = new List<Employee>().AsQueryable().BuildMockDbSet(); // No matching employee
        _mockContext.Setup(c => c.Employees).Returns(employees.Object);

        var command = CreateValidCommand();

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Người dùng không phải là nhân viên.");
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorized_WhenEnterpriseDoesNotMatch()
    {
        // Arrange
        SetupAuthenticatedUser();
        var employee = CreateEmployee();
        var participant = CreateParticipant();
        var interview = CreateScheduledInterview(participant);
        interview.Application.JobPosting.EnterpriseId = Guid.NewGuid(); // Different enterprise

        var employees = new List<Employee> { employee }.AsQueryable().BuildMockDbSet();
        var interviews = new List<Interview> { interview }.AsQueryable().BuildMockDbSet();

        _mockContext.Setup(c => c.Employees).Returns(employees.Object);
        _mockContext.Setup(c => c.Interviews).Returns(interviews.Object);

        var command = CreateValidCommand();

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Bạn không có quyền truy cập hồ sơ ứng tuyển này.");
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenInterviewNotFound()
    {
        // Arrange
        SetupAuthenticatedUser();
        var employee = CreateEmployee();
        var employees = new List<Employee> { employee }.AsQueryable().BuildMockDbSet();
        var interviews = new List<Interview>().AsQueryable().BuildMockDbSet(); // No matching interview

        _mockContext.Setup(c => c.Employees).Returns(employees.Object);
        _mockContext.Setup(c => c.Interviews).Returns(interviews.Object);

        var command = CreateValidCommand();

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage($"Không tìm thấy buổi phỏng vấn với ID {_interviewId}*");
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenInterviewNotInScheduledStatus()
    {
        // Arrange
        SetupAuthenticatedUser();
        var employee = CreateEmployee();
        var participant = CreateParticipant();
        var interview = CreateScheduledInterview(participant);
        interview.Status = InterviewStatus.Completed; // Wrong status

        var employees = new List<Employee> { employee }.AsQueryable().BuildMockDbSet();
        var interviews = new List<Interview> { interview }.AsQueryable().BuildMockDbSet();

        _mockContext.Setup(c => c.Employees).Returns(employees.Object);
        _mockContext.Setup(c => c.Interviews).Returns(interviews.Object);

        var command = CreateValidCommand();

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*Không thể gửi đánh giá*");
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorized_WhenUserIsNotParticipant()
    {
        // Arrange
        SetupAuthenticatedUser();
        var employee = CreateEmployee();
        var otherParticipant = new InterviewParticipant
        {
            Id = Guid.NewGuid(),
            InterviewId = _interviewId,
            EmployeeId = Guid.NewGuid(), // Different employee
            Role = "Interviewer"
        };
        var interview = CreateScheduledInterview(otherParticipant);

        var employees = new List<Employee> { employee }.AsQueryable().BuildMockDbSet();
        var interviews = new List<Interview> { interview }.AsQueryable().BuildMockDbSet();

        _mockContext.Setup(c => c.Employees).Returns(employees.Object);
        _mockContext.Setup(c => c.Interviews).Returns(interviews.Object);

        var command = CreateValidCommand();

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Bạn không phải là người tham gia buổi phỏng vấn này.");
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenFeedbackAlreadySubmitted()
    {
        // Arrange
        SetupAuthenticatedUser();
        var employee = CreateEmployee();
        var participant = CreateParticipant(alreadySubmitted: true);
        var interview = CreateScheduledInterview(participant);

        var employees = new List<Employee> { employee }.AsQueryable().BuildMockDbSet();
        var interviews = new List<Interview> { interview }.AsQueryable().BuildMockDbSet();

        _mockContext.Setup(c => c.Employees).Returns(employees.Object);
        _mockContext.Setup(c => c.Interviews).Returns(interviews.Object);

        var command = CreateValidCommand();

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Bạn đã gửi đánh giá cho buổi phỏng vấn này rồi.");
    }
}
