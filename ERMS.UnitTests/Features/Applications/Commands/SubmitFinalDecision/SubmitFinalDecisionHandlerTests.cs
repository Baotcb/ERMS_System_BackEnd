using ERMS.Application.Features.Applications.Commands.SubmitFinalDecision;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Application;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Organization;
using ERMS.Domain.Entities.Recruitment;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using ApplicationEntity = ERMS.Domain.Entities.Application.Application;
using Xunit;

namespace ERMS.UnitTests.Features.Applications.Commands.SubmitFinalDecision;

public class SubmitFinalDecisionHandlerTests
{
    private readonly Mock<IERMSDbContext> _mockContext;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<ILogger<SubmitFinalDecisionHandler>> _mockLogger;
    private readonly SubmitFinalDecisionHandler _handler;

    // Shared test data
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _enterpriseId = Guid.NewGuid();
    private readonly int _departmentId = 1;
    private readonly Guid _applicationId = Guid.NewGuid();
    private readonly Guid _interviewId = Guid.NewGuid();

    public SubmitFinalDecisionHandlerTests()
    {
        _mockContext = new Mock<IERMSDbContext>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockLogger = new Mock<ILogger<SubmitFinalDecisionHandler>>();
        _handler = new SubmitFinalDecisionHandler(_mockContext.Object, _mockCurrentUserService.Object, _mockLogger.Object);
    }

    private void SetupDepartmentHead()
    {
        _mockCurrentUserService.Setup(s => s.UserId).Returns(_userId);
        _mockCurrentUserService.Setup(s => s.Roles).Returns([AppRoles.DepartmentHead]);
        _mockCurrentUserService.Setup(s => s.GetDepartmentIdAsync()).ReturnsAsync(_departmentId);
        _mockCurrentUserService.Setup(s => s.GetEnterpriseIdAsync()).ReturnsAsync(_enterpriseId);
    }

    private Interview CreateScheduledInterview()
    {
        var jobPosting = new JobPosting
        {
            Id = Guid.NewGuid(),
            DepartmentId = _departmentId,
            EnterpriseId = _enterpriseId
        };
        var application = new ApplicationEntity
        {
            Id = _applicationId,
            Stage = ApplicationStage.InterviewScheduled,
            Status = "Active",
            JobPosting = jobPosting
        };
        var participant = new InterviewParticipant
        {
            Id = Guid.NewGuid(),
            InterviewId = _interviewId,
            EmployeeId = Guid.NewGuid(),
            Role = "Interviewer",
            IsRequired = true,
            ConfirmationStatus = "Confirmed"
        };

        return new Interview
        {
            Id = _interviewId,
            ApplicationId = _applicationId,
            Status = InterviewStatus.Scheduled,
            RoundNumber = 1,
            InterviewType = "Technical",
            ScheduledById = Guid.NewGuid(),
            Application = application,
            Participants = new List<InterviewParticipant> { participant }
        };
    }

    private void SetupDbSets(Interview interview)
    {
        var interviews = new List<Interview> { interview }.AsQueryable().BuildMockDbSet();
        _mockContext.Setup(c => c.Interviews).Returns(interviews.Object);
        _mockContext.Setup(c => c.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Mock<IDbContextTransaction>().Object);
    }

    [Fact]
    public async Task Handle_ShouldRejectApplication_WhenDecisionIsFail()
    {
        // Arrange
        SetupDepartmentHead();
        var interview = CreateScheduledInterview();
        SetupDbSets(interview);

        var command = new SubmitFinalDecisionCommand
        {
            ApplicationId = _applicationId,
            InterviewId = _interviewId,
            Decision = InterviewDecision.Fail,
            OverallRating = 2,
            OverallFeedback = "Did not meet expectations."
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Decision.Should().Be(InterviewDecision.Fail);
        result.ApplicationStage.Should().Be(ApplicationStage.Rejected);
        result.NewInterviewId.Should().BeNull();

        interview.Status.Should().Be(InterviewStatus.Completed);
        interview.CompletedAt.Should().NotBeNull();
        interview.Application.Stage.Should().Be(ApplicationStage.Rejected);
        interview.Application.RejectedAt.Should().NotBeNull();

        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldSetOfferProcessing_WhenDecisionIsPassed()
    {
        // Arrange
        SetupDepartmentHead();
        var interview = CreateScheduledInterview();
        SetupDbSets(interview);

        var command = new SubmitFinalDecisionCommand
        {
            ApplicationId = _applicationId,
            InterviewId = _interviewId,
            Decision = InterviewDecision.Passed,
            OverallRating = 5,
            OverallFeedback = "Excellent candidate."
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Decision.Should().Be(InterviewDecision.Passed);
        result.ApplicationStage.Should().Be(ApplicationStage.OfferProcessing);
        result.NewInterviewId.Should().BeNull();

        interview.Status.Should().Be(InterviewStatus.Completed);
        interview.Application.Stage.Should().Be(ApplicationStage.OfferProcessing);

        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCreateNextRoundInterview_WhenDecisionIsNextRound()
    {
        // Arrange
        SetupDepartmentHead();
        var interview = CreateScheduledInterview();
        SetupDbSets(interview);

        var command = new SubmitFinalDecisionCommand
        {
            ApplicationId = _applicationId,
            InterviewId = _interviewId,
            Decision = InterviewDecision.NextRound,
            OverallFeedback = "Good, but needs further evaluation."
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Decision.Should().Be(InterviewDecision.NextRound);
        result.NewInterviewId.Should().NotBeNull();
        result.NewInterviewId.Should().NotBe(Guid.Empty);

        interview.Status.Should().Be(InterviewStatus.Completed);

        // Verify new interview was added
        _mockContext.Verify(c => c.Interviews.Add(It.Is<Interview>(i =>
            i.RoundNumber == 2 &&
            i.Status == InterviewStatus.PendingSchedule &&
            i.ApplicationId == _applicationId &&
            i.Participants.Count == 1
        )), Times.Once);

        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCreateNextRoundInterview_WithCustomInterviewers_WhenProvided()
    {
        // Arrange
        SetupDepartmentHead();
        var interview = CreateScheduledInterview();
        var newInterviewerId = Guid.NewGuid();
        
        // Mock the new employee to be found in the DB
        var newEmployee = new Employee { Id = newInterviewerId, EnterpriseId = _enterpriseId };
        var employees = new List<Employee> { newEmployee }.AsQueryable().BuildMockDbSet();
        _mockContext.Setup(c => c.Employees).Returns(employees.Object);
        
        SetupDbSets(interview);

        var command = new SubmitFinalDecisionCommand
        {
            ApplicationId = _applicationId,
            InterviewId = _interviewId,
            Decision = InterviewDecision.NextRound,
            NextRoundInterviewerIds = [newInterviewerId]
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.NewInterviewId.Should().NotBeNull();

        // Verify new interview was added with the NEW interviewer, not the old one
        _mockContext.Verify(c => c.Interviews.Add(It.Is<Interview>(i =>
            i.RoundNumber == 2 &&
            i.Participants.Count == 1 &&
            i.Participants.First().EmployeeId == newInterviewerId
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorized_WhenUserIsNotDepartmentHead()
    {
        // Arrange
        _mockCurrentUserService.Setup(s => s.UserId).Returns(_userId);
        _mockCurrentUserService.Setup(s => s.Roles).Returns([AppRoles.Employee]); // Not DeptHead

        var command = new SubmitFinalDecisionCommand
        {
            ApplicationId = _applicationId,
            InterviewId = _interviewId,
            Decision = InterviewDecision.Fail
        };

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Only Department Head can submit the final interview decision.");
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorized_WhenDepartmentDoesNotMatch()
    {
        // Arrange
        SetupDepartmentHead();
        var interview = CreateScheduledInterview();
        interview.Application.JobPosting.DepartmentId = 999; // Different department
        SetupDbSets(interview);

        var command = new SubmitFinalDecisionCommand
        {
            ApplicationId = _applicationId,
            InterviewId = _interviewId,
            Decision = InterviewDecision.Passed
        };

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("You can only make decisions for interviews in your department.");
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenInterviewNotScheduled()
    {
        // Arrange
        SetupDepartmentHead();
        var interview = CreateScheduledInterview();
        interview.Status = InterviewStatus.Completed; // Already completed
        SetupDbSets(interview);

        var command = new SubmitFinalDecisionCommand
        {
            ApplicationId = _applicationId,
            InterviewId = _interviewId,
            Decision = InterviewDecision.Passed
        };

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Cannot submit decision*");
    }
}
