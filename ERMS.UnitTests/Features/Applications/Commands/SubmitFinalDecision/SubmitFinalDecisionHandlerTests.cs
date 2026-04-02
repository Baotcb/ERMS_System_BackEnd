using ERMS.Application.Features.Applications.Commands.SubmitFinalDecision;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Application;
using ERMS.Domain.Entities.Candidate;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Organization;
using ERMS.Domain.Entities.Recruitment;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;

using ApplicationEntity = ERMS.Domain.Entities.Application.Application;

namespace ERMS.UnitTests.Features.Applications.Commands.SubmitFinalDecision;

public class SubmitFinalDecisionHandlerTests
{
    private readonly Mock<IERMSDbContext> _mockContext;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IRejectionEmailService> _mockRejectionEmailService;
    private readonly Mock<ILogger<SubmitFinalDecisionHandler>> _mockLogger;
    private readonly SubmitFinalDecisionHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _enterpriseId = Guid.NewGuid();
    private readonly int _departmentId = 1;
    private readonly Guid _applicationId = Guid.NewGuid();
    private readonly Guid _interviewId = Guid.NewGuid();
    private readonly Guid _candidateId = Guid.NewGuid();
    private readonly Guid _candidateUserId = Guid.NewGuid();

    public SubmitFinalDecisionHandlerTests()
    {
        _mockContext = new Mock<IERMSDbContext>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockRejectionEmailService = new Mock<IRejectionEmailService>();
        _mockLogger = new Mock<ILogger<SubmitFinalDecisionHandler>>();
        _handler = new SubmitFinalDecisionHandler(
            _mockContext.Object,
            _mockCurrentUserService.Object,
            _mockLogger.Object,
            _mockRejectionEmailService.Object);
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
            EnterpriseId = _enterpriseId,
            JobTitle = "Senior Backend Engineer"
        };

        var candidateUser = new User
        {
            Id = _candidateUserId,
            FullName = "Nguyen Van A",
            Email = "candidate@example.com"
        };

        var application = new ApplicationEntity
        {
            Id = _applicationId,
            Stage = ApplicationStage.InterviewScheduled,
            Status = "Active",
            JobPosting = jobPosting,
            CandidateId = _candidateId,
            Candidate = new Candidate
            {
                Id = _candidateId,
                UserId = _candidateUserId,
                User = candidateUser,
                IsDeleted = false
            },
            CVScreeningResult = new CVScreeningResult
            {
                Id = Guid.NewGuid(),
                ApplicationId = _applicationId,
                MissingSkills = "[\"System Design\"]",
                Concerns = "[\"Communication\"]"
            }
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
        _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    [Fact]
    public async Task Handle_ShouldRejectApplication_WhenDecisionIsFail()
    {
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

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Decision.Should().Be(InterviewDecision.Fail);
        result.ApplicationStage.Should().Be(ApplicationStage.Rejected);
        result.NewInterviewId.Should().BeNull();

        interview.Status.Should().Be(InterviewStatus.Completed);
        interview.Application.Stage.Should().Be(ApplicationStage.Rejected);
        interview.Application.RejectedAt.Should().NotBeNull();
        interview.Application.RejectionReason.Should().Be("Did not meet expectations.");

        _mockRejectionEmailService.Verify(
            x => x.SendRejectionEmailAsync(
                It.Is<RejectionEmailContext>(ctx =>
                    ctx.CandidateEmail == "candidate@example.com" &&
                    ctx.CandidateName == "Nguyen Van A" &&
                    ctx.JobTitle == "Senior Backend Engineer" &&
                    ctx.RejectionReason == "Did not meet expectations." &&
                    ctx.SkillGaps != null &&
                    ctx.SkillGaps.Length == 1 &&
                    ctx.SkillGaps[0] == "System Design" &&
                    ctx.Concerns != null &&
                    ctx.Concerns.Length == 1 &&
                    ctx.Concerns[0] == "Communication"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldStillSucceed_WhenRejectionEmailFails()
    {
        SetupDepartmentHead();
        var interview = CreateScheduledInterview();
        SetupDbSets(interview);

        _mockRejectionEmailService
            .Setup(x => x.SendRejectionEmailAsync(It.IsAny<RejectionEmailContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("SMTP down"));

        var command = new SubmitFinalDecisionCommand
        {
            ApplicationId = _applicationId,
            InterviewId = _interviewId,
            Decision = InterviewDecision.Fail,
            OverallRating = 2,
            OverallFeedback = "Did not meet expectations."
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.ApplicationStage.Should().Be(ApplicationStage.Rejected);
        interview.Application.Stage.Should().Be(ApplicationStage.Rejected);
    }

    [Fact]
    public async Task Handle_ShouldSetOfferProcessing_WhenDecisionIsPassed()
    {
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

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Decision.Should().Be(InterviewDecision.Passed);
        result.ApplicationStage.Should().Be(ApplicationStage.OfferProcessing);
        result.NewInterviewId.Should().BeNull();

        interview.Status.Should().Be(InterviewStatus.Completed);
        interview.Application.Stage.Should().Be(ApplicationStage.OfferProcessing);

        _mockRejectionEmailService.Verify(
            x => x.SendRejectionEmailAsync(It.IsAny<RejectionEmailContext>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCreateNextRoundInterview_WhenDecisionIsNextRound()
    {
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

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Decision.Should().Be(InterviewDecision.NextRound);
        result.NewInterviewId.Should().NotBeNull();
        result.NewInterviewId.Should().NotBe(Guid.Empty);

        interview.Status.Should().Be(InterviewStatus.Completed);

        _mockContext.Verify(c => c.Interviews.Add(It.Is<Interview>(i =>
            i.RoundNumber == 2 &&
            i.Status == InterviewStatus.PendingSchedule &&
            i.ApplicationId == _applicationId &&
            i.Participants.Count == 1
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCreateNextRoundInterview_WithCustomInterviewers_WhenProvided()
    {
        SetupDepartmentHead();
        var interview = CreateScheduledInterview();
        var newInterviewerId = Guid.NewGuid();

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

        var result = await _handler.Handle(command, CancellationToken.None);

        result.NewInterviewId.Should().NotBeNull();

        _mockContext.Verify(c => c.Interviews.Add(It.Is<Interview>(i =>
            i.RoundNumber == 2 &&
            i.Participants.Count == 1 &&
            i.Participants.First().EmployeeId == newInterviewerId
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorized_WhenUserIsNotDepartmentHead()
    {
        _mockCurrentUserService.Setup(s => s.UserId).Returns(_userId);
        _mockCurrentUserService.Setup(s => s.GetEnterpriseIdAsync()).ReturnsAsync(_enterpriseId);
        _mockCurrentUserService.Setup(s => s.Roles).Returns([AppRoles.Employee]);

        var command = new SubmitFinalDecisionCommand
        {
            ApplicationId = _applicationId,
            InterviewId = _interviewId,
            Decision = InterviewDecision.Fail
        };

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Chỉ Trưởng phòng mới có quyền đưa ra quyết định phỏng vấn cuối cùng.");
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorized_WhenDepartmentDoesNotMatch()
    {
        SetupDepartmentHead();
        var interview = CreateScheduledInterview();
        interview.Application.JobPosting.DepartmentId = 999;
        SetupDbSets(interview);

        var command = new SubmitFinalDecisionCommand
        {
            ApplicationId = _applicationId,
            InterviewId = _interviewId,
            Decision = InterviewDecision.Passed
        };

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Bạn chỉ có thể đưa ra quyết định cho các buổi phỏng vấn trong phòng ban mình.");
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenInterviewNotScheduled()
    {
        SetupDepartmentHead();
        var interview = CreateScheduledInterview();
        interview.Status = InterviewStatus.Completed;
        SetupDbSets(interview);

        var command = new SubmitFinalDecisionCommand
        {
            ApplicationId = _applicationId,
            InterviewId = _interviewId,
            Decision = InterviewDecision.Passed
        };

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*Không thể gửi quyết định*");
    }
}
