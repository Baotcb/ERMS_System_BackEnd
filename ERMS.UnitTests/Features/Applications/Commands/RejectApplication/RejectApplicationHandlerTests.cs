using ERMS.Application.Features.Applications.Commands.RejectApplication;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Application;
using ERMS.Domain.Entities.Candidate;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Recruitment;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

using ApplicationEntity = ERMS.Domain.Entities.Application.Application;

namespace ERMS.UnitTests.Features.Applications.Commands.RejectApplication;

public class RejectApplicationHandlerTests
{
    private readonly Mock<IERMSDbContext> _contextMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IRejectionEmailService> _rejectionEmailServiceMock;
    private readonly Mock<ILogger<RejectApplicationHandler>> _loggerMock;
    private readonly RejectApplicationHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _enterpriseId = Guid.NewGuid();
    private readonly Guid _applicationId = Guid.NewGuid();
    private readonly Guid _candidateId = Guid.NewGuid();
    private readonly Guid _candidateUserId = Guid.NewGuid();
    private readonly Guid _jobPostingId = Guid.NewGuid();
    private readonly Guid _otherEnterpriseId = Guid.NewGuid();

    public RejectApplicationHandlerTests()
    {
        _contextMock = new Mock<IERMSDbContext>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _rejectionEmailServiceMock = new Mock<IRejectionEmailService>();
        _loggerMock = new Mock<ILogger<RejectApplicationHandler>>();

        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new RejectApplicationHandler(
            _contextMock.Object,
            _currentUserServiceMock.Object,
            _rejectionEmailServiceMock.Object,
            _loggerMock.Object);
    }

    private void SetupAuthenticatedHRManager()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(x => x.Roles).Returns([AppRoles.HRManager]);
        _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(_enterpriseId);
    }

    private ApplicationEntity CreateApplication(string stage = ApplicationStage.Applied, Guid? enterpriseId = null)
    {
        return new ApplicationEntity
        {
            Id = _applicationId,
            JobPostingId = _jobPostingId,
            CandidateId = _candidateId,
            Stage = stage,
            Status = "Active",
            AppliedAt = DateTime.UtcNow.AddDays(-2),
            IsDeleted = false,
            JobPosting = new JobPosting
            {
                Id = _jobPostingId,
                EnterpriseId = enterpriseId ?? _enterpriseId,
                DepartmentId = 1,
                JobTitle = "Backend Developer",
                Description = "Build APIs",
                IsDeleted = false
            },
            Candidate = new Candidate
            {
                Id = _candidateId,
                UserId = _candidateUserId,
                User = new User
                {
                    Id = _candidateUserId,
                    FullName = "Nguyen Van A",
                    Email = "candidate@example.com"
                },
                IsDeleted = false
            },
            CVScreeningResult = new CVScreeningResult
            {
                Id = Guid.NewGuid(),
                ApplicationId = _applicationId,
                MissingSkills = "[\"React\",\"Docker\"]",
                Concerns = "[\"Communication could be stronger\"]"
            }
        };
    }

    private RejectApplicationCommand CreateValidCommand(string rejectionReason = "Thiếu kinh nghiệm thực tiễn phù hợp.")
    {
        return new RejectApplicationCommand
        {
            ApplicationId = _applicationId,
            RejectionReason = rejectionReason
        };
    }

    private void SetupApplicationsDbSet(ApplicationEntity? application)
    {
        var data = application != null
            ? new List<ApplicationEntity> { application }.AsQueryable()
            : new List<ApplicationEntity>().AsQueryable();

        _contextMock.Setup(c => c.Applications).Returns(data.BuildMockDbSet().Object);
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserNotAuthenticated()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        await _handler.Invoking(h => h.Handle(CreateValidCommand(), CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Người dùng chưa được xác thực.");
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenApplicationBelongsToDifferentEnterprise()
    {
        SetupAuthenticatedHRManager();
        SetupApplicationsDbSet(CreateApplication(enterpriseId: _otherEnterpriseId));

        await _handler.Invoking(h => h.Handle(CreateValidCommand(), CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Bạn không có quyền truy cập hồ sơ ứng tuyển này.");
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenStageIsNotAllowed()
    {
        SetupAuthenticatedHRManager();
        SetupApplicationsDbSet(CreateApplication(ApplicationStage.Interviewed));

        await _handler.Invoking(h => h.Handle(CreateValidCommand(), CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("*Không thể từ chối hồ sơ*Giai đoạn hiện tại là 'Interviewed'*");
    }

    [Fact]
    public async Task Handle_ShouldRejectApplicationAndSendEmail()
    {
        SetupAuthenticatedHRManager();
        var application = CreateApplication(ApplicationStage.Reviewing);
        SetupApplicationsDbSet(application);

        var result = await _handler.Handle(
            CreateValidCommand("  Thiếu kinh nghiệm thực tiễn phù hợp.  "),
            CancellationToken.None);

        result.ApplicationId.Should().Be(_applicationId);
        result.PreviousStage.Should().Be(ApplicationStage.Reviewing);
        result.NewStage.Should().Be(ApplicationStage.Rejected);
        result.RejectedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        application.Stage.Should().Be(ApplicationStage.Rejected);
        application.RejectionReason.Should().Be("Thiếu kinh nghiệm thực tiễn phù hợp.");
        application.RejectedById.Should().Be(_userId);
        application.RejectedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        _rejectionEmailServiceMock.Verify(
            x => x.SendRejectionEmailAsync(
                It.Is<RejectionEmailContext>(ctx =>
                    ctx.CandidateEmail == "candidate@example.com" &&
                    ctx.CandidateName == "Nguyen Van A" &&
                    ctx.JobTitle == "Backend Developer" &&
                    ctx.RejectionReason == "Thiếu kinh nghiệm thực tiễn phù hợp." &&
                    ctx.SkillGaps != null &&
                    ctx.SkillGaps.Length == 2 &&
                    ctx.SkillGaps[0] == "React" &&
                    ctx.SkillGaps[1] == "Docker" &&
                    ctx.Concerns != null &&
                    ctx.Concerns.Length == 1 &&
                    ctx.Concerns[0] == "Communication could be stronger"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldStillSucceed_WhenEmailSendingFails()
    {
        SetupAuthenticatedHRManager();
        var application = CreateApplication();
        SetupApplicationsDbSet(application);

        _rejectionEmailServiceMock
            .Setup(x => x.SendRejectionEmailAsync(It.IsAny<RejectionEmailContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("SMTP error"));

        var result = await _handler.Handle(CreateValidCommand(), CancellationToken.None);

        result.NewStage.Should().Be(ApplicationStage.Rejected);
        application.Stage.Should().Be(ApplicationStage.Rejected);
        _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
