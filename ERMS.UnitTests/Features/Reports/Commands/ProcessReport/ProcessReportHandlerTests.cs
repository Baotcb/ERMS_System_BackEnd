using ERMS.Application.Features.Reports.Commands.ProcessReport;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Enterprise;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Constants.System;
using ERMS.Domain.Entities.Enterprise;
using ERMS.Domain.Entities.Recruitment;
using ERMS.Domain.Entities.System;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;

namespace ERMS.UnitTests.Features.Reports.Commands.ProcessReport;

public class ProcessReportHandlerTests
{
    private readonly Mock<IERMSDbContext> _mockContext;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly ProcessReportHandler _handler;
    private readonly Mock<IDbContextTransaction> _mockTransaction;

    public ProcessReportHandlerTests()
    {
        _mockContext = new Mock<IERMSDbContext>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockTransaction = new Mock<IDbContextTransaction>();
        _handler = new ProcessReportHandler(_mockContext.Object, _mockCurrentUserService.Object);
    }

    [Fact]
    public async Task Handle_Dismiss_ShouldSetReportDismissed()
    {
        var adminId = Guid.NewGuid();
        var report = CreatePendingJobPostingReport();

        SetupCommonContext(adminId, [report], [], []);

        await _handler.Handle(new ProcessReportCommand
        {
            ReportId = report.Id,
            Action = "Dismiss",
            AdminNote = "False alarm"
        }, CancellationToken.None);

        report.Status.Should().Be(ReportConstants.Status.Dismissed);
        report.ActionTaken.Should().Be(ReportConstants.ActionTaken.Dismissed);
        report.ResolvedById.Should().Be(adminId);
        report.ResolvedAt.Should().NotBeNull();
        report.AdminNote.Should().Be("False alarm");
    }

    [Fact]
    public async Task Handle_Resolve_ShouldSetReportResolved()
    {
        var adminId = Guid.NewGuid();
        var report = CreatePendingJobPostingReport();

        SetupCommonContext(adminId, [report], [], []);

        await _handler.Handle(new ProcessReportCommand
        {
            ReportId = report.Id,
            Action = "Resolve",
            AdminNote = "Handled manually"
        }, CancellationToken.None);

        report.Status.Should().Be(ReportConstants.Status.Resolved);
        report.ActionTaken.Should().Be(ReportConstants.ActionTaken.Resolved);
    }

    [Fact]
    public async Task Handle_HideJobPosting_ShouldArchiveJobAndResolveAllOpenReportsForEntity()
    {
        var adminId = Guid.NewGuid();
        var jobPostingId = Guid.NewGuid();
        var targetReport = CreatePendingJobPostingReport(jobPostingId);
        var siblingPending = CreatePendingJobPostingReport(jobPostingId);
        var siblingReviewing = CreatePendingJobPostingReport(jobPostingId);
        siblingReviewing.Status = ReportConstants.Status.Reviewing;
        var siblingResolved = CreatePendingJobPostingReport(jobPostingId);
        siblingResolved.Status = ReportConstants.Status.Resolved;
        var otherEntityPending = CreatePendingJobPostingReport(Guid.NewGuid());

        var jobPosting = new JobPosting
        {
            Id = jobPostingId,
            JobTitle = "Bad Posting",
            EnterpriseId = Guid.NewGuid(),
            DepartmentId = 1,
            Description = "Desc",
            CreatedById = Guid.NewGuid(),
            Status = "Published",
            IsDeleted = false
        };

        SetupCommonContext(adminId, [targetReport, siblingPending, siblingReviewing, siblingResolved, otherEntityPending], [jobPosting], []);

        await _handler.Handle(new ProcessReportCommand
        {
            ReportId = targetReport.Id,
            Action = "HideJobPosting",
            AdminNote = "Violation confirmed"
        }, CancellationToken.None);

        jobPosting.Status.Should().Be("Archived");
        targetReport.Status.Should().Be(ReportConstants.Status.Resolved);
        targetReport.ActionTaken.Should().Be(ReportConstants.ActionTaken.HidJobPosting);
        siblingPending.Status.Should().Be(ReportConstants.Status.Resolved);
        siblingPending.ActionTaken.Should().Be(ReportConstants.ActionTaken.HidJobPosting);
        siblingReviewing.Status.Should().Be(ReportConstants.Status.Resolved);
        siblingReviewing.ActionTaken.Should().Be(ReportConstants.ActionTaken.HidJobPosting);
        siblingResolved.ActionTaken.Should().BeNull();
        otherEntityPending.Status.Should().Be(ReportConstants.Status.Pending);
        _mockTransaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SuspendEnterprise_ShouldSuspendEnterpriseAndResolveAllOpenReportsForEntity()
    {
        var adminId = Guid.NewGuid();
        var enterpriseId = Guid.NewGuid();
        var targetReport = CreatePendingEnterpriseReport(enterpriseId);
        var siblingPending = CreatePendingEnterpriseReport(enterpriseId);
        var siblingReviewing = CreatePendingEnterpriseReport(enterpriseId);
        siblingReviewing.Status = ReportConstants.Status.Reviewing;
        var otherEntityPending = CreatePendingEnterpriseReport(Guid.NewGuid());

        var enterprise = new Enterprise
        {
            Id = enterpriseId,
            EnterpriseName = "Acme",
            EnterpriseCode = "ACM",
            SubscriptionPlanId = Guid.NewGuid(),
            Status = EnterpriseStatus.Active,
            IsDeleted = false
        };

        SetupCommonContext(adminId, [targetReport, siblingPending, siblingReviewing, otherEntityPending], [], [enterprise]);

        await _handler.Handle(new ProcessReportCommand
        {
            ReportId = targetReport.Id,
            Action = "SuspendEnterprise",
            AdminNote = "Serious violation"
        }, CancellationToken.None);

        enterprise.Status.Should().Be(EnterpriseStatus.Suspended);
        targetReport.Status.Should().Be(ReportConstants.Status.Resolved);
        targetReport.ActionTaken.Should().Be(ReportConstants.ActionTaken.SuspendedEnterprise);
        siblingPending.Status.Should().Be(ReportConstants.Status.Resolved);
        siblingReviewing.Status.Should().Be(ReportConstants.Status.Resolved);
        siblingReviewing.ActionTaken.Should().Be(ReportConstants.ActionTaken.SuspendedEnterprise);
        otherEntityPending.Status.Should().Be(ReportConstants.Status.Pending);
    }

    [Fact]
    public async Task Handle_ShouldThrowKeyNotFound_WhenReportNotFound()
    {
        SetupCommonContext(Guid.NewGuid(), [], [], []);

        var act = async () => await _handler.Handle(new ProcessReportCommand
        {
            ReportId = Guid.NewGuid(),
            Action = "Dismiss"
        }, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowInvalidOperation_WhenReportAlreadyProcessed()
    {
        var processedReport = CreatePendingJobPostingReport();
        processedReport.Status = ReportConstants.Status.Resolved;

        SetupCommonContext(Guid.NewGuid(), [processedReport], [], []);

        var act = async () => await _handler.Handle(new ProcessReportCommand
        {
            ReportId = processedReport.Id,
            Action = "Resolve"
        }, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_HideJobPosting_ShouldThrowInvalidOperation_WhenReportTargetsEnterprise()
    {
        var enterpriseReport = CreatePendingEnterpriseReport();
        SetupCommonContext(Guid.NewGuid(), [enterpriseReport], [], []);

        var act = async () => await _handler.Handle(new ProcessReportCommand
        {
            ReportId = enterpriseReport.Id,
            Action = "HideJobPosting"
        }, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorized_WhenCurrentUserMissing()
    {
        _mockCurrentUserService.Setup(x => x.UserId).Returns((Guid?)null);
        _mockContext.Setup(x => x.Reports).Returns(new List<Report>().AsQueryable().BuildMockDbSet().Object);

        var act = async () => await _handler.Handle(new ProcessReportCommand
        {
            ReportId = Guid.NewGuid(),
            Action = "Dismiss"
        }, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    private void SetupCommonContext(
        Guid adminId,
        IEnumerable<Report> reports,
        IEnumerable<JobPosting> jobPostings,
        IEnumerable<Enterprise> enterprises)
    {
        _mockCurrentUserService.Setup(x => x.UserId).Returns(adminId);
        _mockCurrentUserService.Setup(x => x.Roles).Returns([AppRoles.Admin]);
        _mockContext.Setup(x => x.Reports).Returns(reports.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.JobPostings).Returns(jobPostings.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Enterprises).Returns(enterprises.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockTransaction.Object);
        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private static Report CreatePendingJobPostingReport(Guid? entityId = null)
    {
        return new Report
        {
            Id = Guid.NewGuid(),
            ReportedById = Guid.NewGuid(),
            EntityType = ReportConstants.EntityType.JobPosting,
            EntityId = entityId ?? Guid.NewGuid(),
            Reason = ReportConstants.Reason.FraudulentInfo,
            Status = ReportConstants.Status.Pending
        };
    }

    private static Report CreatePendingEnterpriseReport(Guid? entityId = null)
    {
        return new Report
        {
            Id = Guid.NewGuid(),
            ReportedById = Guid.NewGuid(),
            EntityType = ReportConstants.EntityType.Enterprise,
            EntityId = entityId ?? Guid.NewGuid(),
            Reason = ReportConstants.Reason.Other,
            Status = ReportConstants.Status.Pending
        };
    }
}
