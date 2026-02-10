using ERMS.Application.Features.Applications.Commands.ConfirmInterviewSchedule;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Application;
using ERMS.Domain.Entities.Candidate; // Add namespace for Candidate
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Organization; // Add namespace for Employee
using ERMS.Domain.Entities.Recruitment;
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
    private readonly Mock<IGoogleCalendarService> _mockGoogleCalendarService;
    private readonly Mock<ILogger<ConfirmInterviewScheduleHandler>> _mockLogger;
    private readonly ConfirmInterviewScheduleHandler _handler;

    public ConfirmInterviewScheduleHandlerTests()
    {
        _mockContext = new Mock<IERMSDbContext>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockGoogleCalendarService = new Mock<IGoogleCalendarService>();
        _mockLogger = new Mock<ILogger<ConfirmInterviewScheduleHandler>>();
        _handler = new ConfirmInterviewScheduleHandler(
            _mockContext.Object, 
            _mockCurrentUserService.Object, 
            _mockGoogleCalendarService.Object, 
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ShouldConfirmSchedule_WhenUserIsHRManager()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var enterpriseId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();
        var interviewId = Guid.NewGuid();

        _mockCurrentUserService.Setup(s => s.UserId).Returns(userId);
        _mockCurrentUserService.Setup(s => s.Roles).Returns([AppRoles.HRManager]);
        _mockCurrentUserService.Setup(s => s.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

        var jobPosting = new JobPosting 
        { 
            Id = Guid.NewGuid(), 
            EnterpriseId = enterpriseId,
            JobTitle = "Developer"
        };

        var candidateUser = new User { FullName = "Candidate Name", Email = "candidate@test.com" };
        var candidate = new Candidate { User = candidateUser };
        
        var application = new ApplicationEntity 
        { 
            Id = applicationId, 
            JobPosting = jobPosting, 
            Candidate = candidate,
            Stage = ApplicationStage.Shortlisted
        };

        var interviewerUser = new User { Email = "interviewer@test.com" };
        var interviewer = new Employee { User = interviewerUser };
        var participant = new InterviewParticipant { Employee = interviewer };

        var interview = new Interview 
        { 
            Id = interviewId, 
            ApplicationId = applicationId, 
            Application = application,
            Status = InterviewStatus.PendingSchedule,
            Participants = [participant]
        };

        var interviews = new List<Interview> { interview }.AsQueryable().BuildMockDbSet();
        _mockContext.Setup(c => c.Interviews).Returns(interviews.Object);
        
        _mockContext.Setup(c => c.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Mock<IDbContextTransaction>().Object);

        _mockGoogleCalendarService.Setup(s => s.CreateMeetingAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<List<string>>()))
            .ReturnsAsync("https://meet.google.com/test");

        var command = new ConfirmInterviewScheduleCommand
        {
            ApplicationId = applicationId,
            ScheduledAt = DateTime.UtcNow.AddDays(1),
            Duration = 60,
            Location = "Online"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(InterviewStatus.Scheduled);
        result.MeetingLink.Should().Be("https://meet.google.com/test");

        interview.Status.Should().Be(InterviewStatus.Scheduled);
        interview.ScheduledAt.Should().Be(command.ScheduledAt);
        application.Stage.Should().Be(ApplicationStage.InterviewScheduled);

        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
