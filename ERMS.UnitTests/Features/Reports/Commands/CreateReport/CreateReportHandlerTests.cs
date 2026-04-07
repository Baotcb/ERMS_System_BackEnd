using ERMS.Application.Features.Reports.Commands.CreateReport;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Constants.System;
using ERMS.Domain.Entities.Enterprise;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Recruitment;
using ERMS.Domain.Entities.System;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace ERMS.UnitTests.Features.Reports.Commands.CreateReport;

public class CreateReportHandlerTests
{
    private readonly Mock<IERMSDbContext> _mockContext;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<UserManager<User>> _mockUserManager;
    private readonly CreateReportHandler _handler;

    public CreateReportHandlerTests()
    {
        _mockContext = new Mock<IERMSDbContext>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockUserManager = MockUserManager();
        _handler = new CreateReportHandler(_mockContext.Object, _mockCurrentUserService.Object, _mockUserManager.Object);
    }

    [Fact]
    public async Task Handle_ShouldCreateReportForJobPosting_AndNotifyAdmins()
    {
        var reporterId = Guid.NewGuid();
        var jobPostingId = Guid.NewGuid();
        var admin1 = new User { Id = Guid.NewGuid(), FullName = "Admin One", Email = "admin1@test.local" };
        var admin2 = new User { Id = Guid.NewGuid(), FullName = "Admin Two", Email = "admin2@test.local" };

        Report? addedReport = null;
        IEnumerable<Notification>? addedNotifications = null;

        var reportsDbSet = new List<Report>().AsQueryable().BuildMockDbSet();
        reportsDbSet.Setup(x => x.Add(It.IsAny<Report>()))
            .Callback<Report>(r => addedReport = r);

        var notificationsDbSet = new List<Notification>().AsQueryable().BuildMockDbSet();
        notificationsDbSet.Setup(x => x.AddRange(It.IsAny<IEnumerable<Notification>>()))
            .Callback<IEnumerable<Notification>>(n => addedNotifications = n.ToList());

        _mockCurrentUserService.Setup(x => x.UserId).Returns(reporterId);
        _mockCurrentUserService.Setup(x => x.Roles).Returns([AppRoles.Candidate]);

        _mockContext.Setup(x => x.JobPostings).Returns(new List<JobPosting>
        {
            new() { Id = jobPostingId, IsDeleted = false, JobTitle = "Backend Engineer", EnterpriseId = Guid.NewGuid(), DepartmentId = 1, Description = "Desc", CreatedById = Guid.NewGuid() }
        }.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Enterprises).Returns(new List<Enterprise>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Reports).Returns(reportsDbSet.Object);
        _mockContext.Setup(x => x.Notifications).Returns(notificationsDbSet.Object);
        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mockUserManager.Setup(x => x.GetUsersInRoleAsync(AppRoles.Admin)).ReturnsAsync([admin1, admin2]);

        var command = new CreateReportCommand
        {
            EntityType = ReportConstants.EntityType.JobPosting,
            EntityId = jobPostingId,
            Reason = ReportConstants.Reason.FraudulentInfo,
            Description = "Suspicious posting"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBe(Guid.Empty);
        addedReport.Should().NotBeNull();
        addedReport!.Id.Should().Be(result);
        addedReport.ReportedById.Should().Be(reporterId);
        addedReport.EntityType.Should().Be(ReportConstants.EntityType.JobPosting);
        addedReport.EntityId.Should().Be(jobPostingId);
        addedReport.Status.Should().Be(ReportConstants.Status.Pending);

        addedNotifications.Should().NotBeNull();
        var notifications = addedNotifications!.ToList();
        notifications.Should().HaveCount(2);
        notifications.Should().OnlyContain(n => n.EntityType == "Report" && n.EntityId == result);
        notifications.Should().OnlyContain(n => n.ActionUrl == "/admin/reports");
    }

    [Fact]
    public async Task Handle_ShouldCreateReportForEnterprise()
    {
        var reporterId = Guid.NewGuid();
        var enterpriseId = Guid.NewGuid();

        Report? addedReport = null;
        var reportsDbSet = new List<Report>().AsQueryable().BuildMockDbSet();
        reportsDbSet.Setup(x => x.Add(It.IsAny<Report>()))
            .Callback<Report>(r => addedReport = r);

        _mockCurrentUserService.Setup(x => x.UserId).Returns(reporterId);
        _mockCurrentUserService.Setup(x => x.Roles).Returns([AppRoles.Candidate]);

        _mockContext.Setup(x => x.JobPostings).Returns(new List<JobPosting>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Enterprises).Returns(new List<Enterprise>
        {
            new() { Id = enterpriseId, EnterpriseName = "Acme", EnterpriseCode = "ACM", SubscriptionPlanId = Guid.NewGuid(), IsDeleted = false }
        }.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Reports).Returns(reportsDbSet.Object);
        _mockContext.Setup(x => x.Notifications).Returns(new List<Notification>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mockUserManager.Setup(x => x.GetUsersInRoleAsync(AppRoles.Admin)).ReturnsAsync(new List<User>());

        var result = await _handler.Handle(new CreateReportCommand
        {
            EntityType = ReportConstants.EntityType.Enterprise,
            EntityId = enterpriseId,
            Reason = ReportConstants.Reason.Other,
            Description = "Need review"
        }, CancellationToken.None);

        result.Should().NotBe(Guid.Empty);
        addedReport.Should().NotBeNull();
        addedReport!.EntityType.Should().Be(ReportConstants.EntityType.Enterprise);
    }

    [Fact]
    public async Task Handle_ShouldThrowKeyNotFound_WhenEntityNotFound()
    {
        _mockCurrentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid());
        _mockCurrentUserService.Setup(x => x.Roles).Returns([AppRoles.Candidate]);
        _mockContext.Setup(x => x.JobPostings).Returns(new List<JobPosting>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Enterprises).Returns(new List<Enterprise>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Reports).Returns(new List<Report>().AsQueryable().BuildMockDbSet().Object);

        var act = async () => await _handler.Handle(new CreateReportCommand
        {
            EntityType = ReportConstants.EntityType.JobPosting,
            EntityId = Guid.NewGuid(),
            Reason = ReportConstants.Reason.FakeContactInfo
        }, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowInvalidOperation_WhenDuplicatePendingReportExists()
    {
        var reporterId = Guid.NewGuid();
        var entityId = Guid.NewGuid();

        _mockCurrentUserService.Setup(x => x.UserId).Returns(reporterId);
        _mockCurrentUserService.Setup(x => x.Roles).Returns([AppRoles.Candidate]);
        _mockContext.Setup(x => x.JobPostings).Returns(new List<JobPosting>
        {
            new() { Id = entityId, IsDeleted = false, JobTitle = "Existing", EnterpriseId = Guid.NewGuid(), DepartmentId = 1, Description = "Desc", CreatedById = Guid.NewGuid() }
        }.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Enterprises).Returns(new List<Enterprise>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Reports).Returns(new List<Report>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ReportedById = reporterId,
                EntityType = ReportConstants.EntityType.JobPosting,
                EntityId = entityId,
                Reason = ReportConstants.Reason.FraudulentInfo,
                Status = ReportConstants.Status.Pending
            }
        }.AsQueryable().BuildMockDbSet().Object);

        var act = async () => await _handler.Handle(new CreateReportCommand
        {
            EntityType = ReportConstants.EntityType.JobPosting,
            EntityId = entityId,
            Reason = ReportConstants.Reason.InappropriateContent
        }, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowInvalidOperation_WhenDuplicateReviewingReportExists()
    {
        var reporterId = Guid.NewGuid();
        var entityId = Guid.NewGuid();

        _mockCurrentUserService.Setup(x => x.UserId).Returns(reporterId);
        _mockCurrentUserService.Setup(x => x.Roles).Returns([AppRoles.Candidate]);
        _mockContext.Setup(x => x.JobPostings).Returns(new List<JobPosting>
        {
            new() { Id = entityId, IsDeleted = false, JobTitle = "Existing", EnterpriseId = Guid.NewGuid(), DepartmentId = 1, Description = "Desc", CreatedById = Guid.NewGuid() }
        }.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Enterprises).Returns(new List<Enterprise>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Reports).Returns(new List<Report>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ReportedById = reporterId,
                EntityType = ReportConstants.EntityType.JobPosting,
                EntityId = entityId,
                Reason = ReportConstants.Reason.FraudulentInfo,
                Status = ReportConstants.Status.Reviewing
            }
        }.AsQueryable().BuildMockDbSet().Object);

        var act = async () => await _handler.Handle(new CreateReportCommand
        {
            EntityType = ReportConstants.EntityType.JobPosting,
            EntityId = entityId,
            Reason = ReportConstants.Reason.InappropriateContent
        }, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorized_WhenUserIsNotCandidate()
    {
        _mockCurrentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid());
        _mockCurrentUserService.Setup(x => x.Roles).Returns([AppRoles.Admin]);

        var act = async () => await _handler.Handle(new CreateReportCommand
        {
            EntityType = ReportConstants.EntityType.Enterprise,
            EntityId = Guid.NewGuid(),
            Reason = ReportConstants.Reason.Other
        }, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorized_WhenCurrentUserIsMissing()
    {
        _mockCurrentUserService.Setup(x => x.UserId).Returns((Guid?)null);

        var act = async () => await _handler.Handle(new CreateReportCommand
        {
            EntityType = ReportConstants.EntityType.Enterprise,
            EntityId = Guid.NewGuid(),
            Reason = ReportConstants.Reason.Other
        }, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    private static Mock<UserManager<User>> MockUserManager()
    {
        var store = new Mock<IUserStore<User>>();
        return new Mock<UserManager<User>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }
}
