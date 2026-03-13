using ERMS.Application.Features.Applications.Commands.ConfirmInterviewSchedule;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Application;
using ERMS.Domain.Entities.Candidate;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Organization;
using ERMS.Domain.Entities.Recruitment;
using ERMS.Domain.Enums;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using ApplicationEntity = ERMS.Domain.Entities.Application.Application;
using Xunit;

namespace ERMS.UnitTests.Features.Applications.Commands.ConfirmInterviewSchedule;

public class ConfirmInterviewScheduleHandlerTests
{
    private readonly Mock<IERMSDbContext> _mockContext;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IZoomService> _mockZoomService;
    private readonly Mock<ICalendarService> _mockCalendarService;
    private readonly Mock<ILogger<ConfirmInterviewScheduleHandler>> _mockLogger;
    private readonly ConfirmInterviewScheduleHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _enterpriseId = Guid.NewGuid();
    private readonly Guid _applicationId = Guid.NewGuid();
    private readonly Guid _interviewId = Guid.NewGuid();

    public ConfirmInterviewScheduleHandlerTests()
    {
        _mockContext = new Mock<IERMSDbContext>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockZoomService = new Mock<IZoomService>();
        _mockCalendarService = new Mock<ICalendarService>();
        _mockLogger = new Mock<ILogger<ConfirmInterviewScheduleHandler>>();
        _handler = new ConfirmInterviewScheduleHandler(
            _mockContext.Object,
            _mockCurrentUserService.Object,
            _mockEmailService.Object,
            _mockZoomService.Object,
            _mockCalendarService.Object,
            _mockLogger.Object);

            _mockCalendarService
            .Setup(c => c.CreateICalendarEvent(It.IsAny<CalendarEventRequest>()))
            .Returns("BEGIN:VCALENDAR\nEND:VCALENDAR");
    }

    private void SetupValidUser()
    {
        _mockCurrentUserService.Setup(s => s.UserId).Returns(_userId);
        _mockCurrentUserService.Setup(s => s.Roles).Returns([AppRoles.HRManager]);
        _mockCurrentUserService.Setup(s => s.GetEnterpriseIdAsync()).ReturnsAsync(_enterpriseId);
    }

    private Interview CreateTestInterview()
    {
        var jobPosting = new JobPosting
        {
            Id = Guid.NewGuid(),
            EnterpriseId = _enterpriseId,
            JobTitle = "Software Developer"
        };

        var candidateUser = new User { FullName = "Jane Candidate", Email = "jane@candidate.com" };
        var candidate = new Candidate { User = candidateUser };

        var application = new ApplicationEntity
        {
            Id = _applicationId,
            JobPosting = jobPosting,
            Candidate = candidate,
            Stage = ApplicationStage.Shortlisted
        };

        var interviewerUser = new User { Email = "interviewer@company.com" };
        var interviewer = new Employee { User = interviewerUser };
        var participant = new InterviewParticipant { Employee = interviewer };

        return new Interview
        {
            Id = _interviewId,
            ApplicationId = _applicationId,
            Application = application,
            Status = InterviewStatus.PendingSchedule,
            Participants = [participant]
        };
    }

    private void SetupMockContext(Interview interview)
    {
        var interviews = new List<Interview> { interview }.AsQueryable().BuildMockDbSet();
        _mockContext.Setup(c => c.Interviews).Returns(interviews.Object);
        _mockContext.Setup(c => c.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Mock<IDbContextTransaction>().Object);
    }

    [Fact]
    public async Task Handle_ShouldConfirmOnlineSchedule_WhenMeetingLinkProvided()
    {
        // Arrange
        SetupValidUser();
        var interview = CreateTestInterview();
        SetupMockContext(interview);

        var command = new ConfirmInterviewScheduleCommand
        {
            ApplicationId = _applicationId,
            InterviewFormat = InterviewFormat.Online,
            ScheduledAt = DateTime.UtcNow.AddDays(1),
            Duration = 60,
            MeetingLink = "https://meet.google.com/abc-defg-hij"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(InterviewStatus.Scheduled);
        result.InterviewFormat.Should().Be(InterviewFormat.Online);
        result.MeetingLink.Should().Be("https://meet.google.com/abc-defg-hij");

        interview.Status.Should().Be(InterviewStatus.Scheduled);
        interview.MeetingLink.Should().Be("https://meet.google.com/abc-defg-hij");
        interview.Application.Stage.Should().Be(ApplicationStage.InterviewScheduled);

        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldConfirmOfflineSchedule_WithLocationAndNoMeetingLink()
    {
        // Arrange
        SetupValidUser();
        var interview = CreateTestInterview();
        SetupMockContext(interview);

        var command = new ConfirmInterviewScheduleCommand
        {
            ApplicationId = _applicationId,
            InterviewFormat = InterviewFormat.Offline,
            ScheduledAt = DateTime.UtcNow.AddDays(2),
            Duration = 90,
            Location = "Building A, Room 301"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(InterviewStatus.Scheduled);
        result.InterviewFormat.Should().Be(InterviewFormat.Offline);
        result.Location.Should().Be("Building A, Room 301");
        result.MeetingLink.Should().BeNull();

        interview.MeetingLink.Should().BeNull();
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldSendEmailToCandidateAndInterviewers_WhenScheduleConfirmed()
    {
        // Arrange
        SetupValidUser();
        var interview = CreateTestInterview();
        SetupMockContext(interview);

        var command = new ConfirmInterviewScheduleCommand
        {
            ApplicationId = _applicationId,
            InterviewFormat = InterviewFormat.Online,
            ScheduledAt = DateTime.UtcNow.AddDays(1),
            Duration = 60,
            MeetingLink = "https://zoom.us/j/123456"
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert: email sent to candidate + 1 interviewer = 2 calls
        _mockEmailService.Verify(
            e => e.SendEmailWithAttachmentAsync("jane@candidate.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, byte[]>>()),
            Times.Once);

        _mockEmailService.Verify(
            e => e.SendEmailWithAttachmentAsync("interviewer@company.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, byte[]>>()),
            Times.Once);

        _mockEmailService.Verify(
            e => e.SendEmailWithAttachmentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, byte[]>>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_ShouldNotFail_WhenEmailServiceThrows()
    {
        // Arrange
        SetupValidUser();
        var interview = CreateTestInterview();
        SetupMockContext(interview);

        _mockEmailService
            .Setup(e => e.SendEmailWithAttachmentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, byte[]>>()))
            .ThrowsAsync(new Exception("SMTP connection failed"));

        var command = new ConfirmInterviewScheduleCommand
        {
            ApplicationId = _applicationId,
            InterviewFormat = InterviewFormat.Online,
            ScheduledAt = DateTime.UtcNow.AddDays(1),
            Duration = 60,
            MeetingLink = "https://zoom.us/j/123456"
        };

        // Act — should NOT throw even though email fails
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserNotHRManager()
    {
        // Arrange
        _mockCurrentUserService.Setup(s => s.UserId).Returns(_userId);
        _mockCurrentUserService.Setup(s => s.Roles).Returns(["Employee"]);

        var command = new ConfirmInterviewScheduleCommand
        {
            ApplicationId = _applicationId,
            ScheduledAt = DateTime.UtcNow.AddDays(1),
            Duration = 60
        };

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*HR Manager*");
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenEnterpriseMismatch()
    {
        // Arrange
        SetupValidUser();
        var interview = CreateTestInterview();
        // Tamper enterprise to cause mismatch
        interview.Application.JobPosting.EnterpriseId = Guid.NewGuid();
        SetupMockContext(interview);

        var command = new ConfirmInterviewScheduleCommand
        {
            ApplicationId = _applicationId,
            InterviewFormat = InterviewFormat.Online,
            ScheduledAt = DateTime.UtcNow.AddDays(1),
            Duration = 60,
            MeetingLink = "https://zoom.us/j/123456"
        };

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*quyền*");
    }
}
