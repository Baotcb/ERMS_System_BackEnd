using ERMS.Application.Features.Reports.Queries.GetAllReports;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.System;
using ERMS.Domain.Entities.Enterprise;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Recruitment;
using ERMS.Domain.Entities.System;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace ERMS.UnitTests.Features.Reports.Queries.GetAllReports;

public class GetAllReportsHandlerTests
{
    private readonly Mock<IERMSDbContext> _mockContext;
    private readonly GetAllReportsHandler _handler;

    public GetAllReportsHandlerTests()
    {
        _mockContext = new Mock<IERMSDbContext>();
        _handler = new GetAllReportsHandler(_mockContext.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnPaginatedResults()
    {
        var data = BuildSampleData();
        SetupContext(data.reports, data.jobPostings, data.enterprises);

        var result = await _handler.Handle(new GetAllReportsQuery
        {
            Page = 1,
            PageSize = 1
        }, CancellationToken.None);

        result.TotalCount.Should().Be(3);
        result.Items.Should().HaveCount(1);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(1);
        result.TotalPages.Should().Be(3);
        result.Items[0].EntityName.Should().Be("Beta Corp");
    }

    [Fact]
    public async Task Handle_ShouldFilterByStatus()
    {
        var data = BuildSampleData();
        SetupContext(data.reports, data.jobPostings, data.enterprises);

        var result = await _handler.Handle(new GetAllReportsQuery
        {
            Status = ReportConstants.Status.Pending,
            Page = 1,
            PageSize = 10
        }, CancellationToken.None);

        result.Items.Should().OnlyContain(x => x.Status == ReportConstants.Status.Pending);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ShouldFilterByEntityType()
    {
        var data = BuildSampleData();
        SetupContext(data.reports, data.jobPostings, data.enterprises);

        var result = await _handler.Handle(new GetAllReportsQuery
        {
            EntityType = ReportConstants.EntityType.JobPosting,
            Page = 1,
            PageSize = 10
        }, CancellationToken.None);

        result.TotalCount.Should().Be(3);
        result.Items.Should().OnlyContain(x => x.EntityType == ReportConstants.EntityType.JobPosting);
    }

    [Fact]
    public async Task Handle_ShouldSearchByEntityName()
    {
        var data = BuildSampleData();
        SetupContext(data.reports, data.jobPostings, data.enterprises);

        var result = await _handler.Handle(new GetAllReportsQuery
        {
            Search = "acme",
            Page = 1,
            PageSize = 10
        }, CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.Items.Should().OnlyContain(x => x.EntityName.Contains("Acme"));
    }

    [Fact]
    public async Task Handle_ShouldIncludeReportCountForEntity()
    {
        var data = BuildSampleData();
        SetupContext(data.reports, data.jobPostings, data.enterprises);

        var result = await _handler.Handle(new GetAllReportsQuery
        {
            Page = 1,
            PageSize = 10
        }, CancellationToken.None);

        result.Items
            .Where(x => x.EntityName == "Acme Backend")
            .Should()
            .OnlyContain(x => x.ReportCountForEntity == 2);
    }

    private void SetupContext(
        IEnumerable<Report> reports,
        IEnumerable<JobPosting> jobPostings,
        IEnumerable<Enterprise> enterprises)
    {
        _mockContext.Setup(x => x.Reports).Returns(reports.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.JobPostings).Returns(jobPostings.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Enterprises).Returns(enterprises.AsQueryable().BuildMockDbSet().Object);
    }

    private static (List<Report> reports, List<JobPosting> jobPostings, List<Enterprise> enterprises) BuildSampleData()
    {
        var reporterA = new User { Id = Guid.NewGuid(), FullName = "Alice", Email = "alice@test.local" };
        var reporterB = new User { Id = Guid.NewGuid(), FullName = "Bob", Email = "bob@test.local" };
        var resolver = new User { Id = Guid.NewGuid(), FullName = "Admin", Email = "admin@test.local" };

        var job1 = new JobPosting
        {
            Id = Guid.NewGuid(),
            JobTitle = "Acme Backend",
            EnterpriseId = Guid.NewGuid(),
            DepartmentId = 1,
            Description = "Desc",
            CreatedById = Guid.NewGuid(),
            IsDeleted = false
        };
        var job2 = new JobPosting
        {
            Id = Guid.NewGuid(),
            JobTitle = "Beta Corp",
            EnterpriseId = Guid.NewGuid(),
            DepartmentId = 1,
            Description = "Desc",
            CreatedById = Guid.NewGuid(),
            IsDeleted = false
        };

        var enterprise = new Enterprise
        {
            Id = Guid.NewGuid(),
            EnterpriseName = "Acme Holdings",
            EnterpriseCode = "ACM",
            SubscriptionPlanId = Guid.NewGuid(),
            IsDeleted = false
        };

        var report1 = new Report
        {
            Id = Guid.NewGuid(),
            ReportedById = reporterA.Id,
            ReportedBy = reporterA,
            EntityType = ReportConstants.EntityType.JobPosting,
            EntityId = job1.Id,
            Reason = ReportConstants.Reason.FraudulentInfo,
            Description = "Issue 1",
            Status = ReportConstants.Status.Pending,
            CreatedAt = new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc)
        };
        var report2 = new Report
        {
            Id = Guid.NewGuid(),
            ReportedById = reporterB.Id,
            ReportedBy = reporterB,
            EntityType = ReportConstants.EntityType.JobPosting,
            EntityId = job1.Id,
            Reason = ReportConstants.Reason.InappropriateContent,
            Description = "Issue 2",
            Status = ReportConstants.Status.Resolved,
            ResolvedById = resolver.Id,
            ResolvedBy = resolver,
            ResolvedAt = new DateTime(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc),
            CreatedAt = new DateTime(2026, 4, 1, 11, 0, 0, DateTimeKind.Utc)
        };
        var report3 = new Report
        {
            Id = Guid.NewGuid(),
            ReportedById = reporterA.Id,
            ReportedBy = reporterA,
            EntityType = ReportConstants.EntityType.JobPosting,
            EntityId = job2.Id,
            Reason = ReportConstants.Reason.Other,
            Description = "Issue 3",
            Status = ReportConstants.Status.Pending,
            CreatedAt = new DateTime(2026, 4, 1, 13, 0, 0, DateTimeKind.Utc)
        };

        return ([report1, report2, report3], [job1, job2], [enterprise]);
    }
}
