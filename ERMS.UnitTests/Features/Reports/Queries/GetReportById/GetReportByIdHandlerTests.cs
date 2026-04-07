using ERMS.Application.Features.Reports.Queries.GetReportById;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.System;
using ERMS.Domain.Entities.Enterprise;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Recruitment;
using ERMS.Domain.Entities.System;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace ERMS.UnitTests.Features.Reports.Queries.GetReportById;

public class GetReportByIdHandlerTests
{
    private readonly Mock<IERMSDbContext> _mockContext;
    private readonly GetReportByIdHandler _handler;

    public GetReportByIdHandlerTests()
    {
        _mockContext = new Mock<IERMSDbContext>();
        _handler = new GetReportByIdHandler(_mockContext.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnReportDetailWithRelatedReports_ForJobPosting()
    {
        var reporter = new User { Id = Guid.NewGuid(), FullName = "Candidate A", Email = "a@test.local" };
        var resolver = new User { Id = Guid.NewGuid(), FullName = "Admin A", Email = "admin@test.local" };
        var jobPosting = new JobPosting
        {
            Id = Guid.NewGuid(),
            JobTitle = "Backend Engineer",
            EnterpriseId = Guid.NewGuid(),
            DepartmentId = 1,
            Description = "Desc",
            CreatedById = Guid.NewGuid(),
            IsDeleted = false
        };

        var report = new Report
        {
            Id = Guid.NewGuid(),
            ReportedById = reporter.Id,
            ReportedBy = reporter,
            EntityType = ReportConstants.EntityType.JobPosting,
            EntityId = jobPosting.Id,
            Reason = ReportConstants.Reason.FraudulentInfo,
            Description = "Main report",
            Status = ReportConstants.Status.Resolved,
            ResolvedById = resolver.Id,
            ResolvedBy = resolver,
            ResolvedAt = new DateTime(2026, 4, 2, 10, 0, 0, DateTimeKind.Utc),
            CreatedAt = new DateTime(2026, 4, 2, 9, 0, 0, DateTimeKind.Utc)
        };

        var related = new Report
        {
            Id = Guid.NewGuid(),
            ReportedById = Guid.NewGuid(),
            ReportedBy = new User { Id = Guid.NewGuid(), FullName = "Candidate B", Email = "b@test.local" },
            EntityType = ReportConstants.EntityType.JobPosting,
            EntityId = jobPosting.Id,
            Reason = ReportConstants.Reason.Other,
            Status = ReportConstants.Status.Pending,
            CreatedAt = new DateTime(2026, 4, 2, 11, 0, 0, DateTimeKind.Utc)
        };

        _mockContext.Setup(x => x.Reports).Returns(new List<Report> { report, related }.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.JobPostings).Returns(new List<JobPosting> { jobPosting }.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Enterprises).Returns(new List<Enterprise>().AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetReportByIdQuery { Id = report.Id }, CancellationToken.None);

        result.Id.Should().Be(report.Id);
        result.EntityName.Should().Be(jobPosting.JobTitle);
        result.ReportedByName.Should().Be(reporter.FullName);
        result.ResolvedByName.Should().Be(resolver.FullName);
        result.RelatedReports.Should().HaveCount(1);
        result.RelatedReports[0].Id.Should().Be(related.Id);
        result.EntityDetails.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnReportDetailWithEntityName_ForEnterprise()
    {
        var reporter = new User { Id = Guid.NewGuid(), FullName = "Candidate A", Email = "a@test.local" };
        var enterprise = new Enterprise
        {
            Id = Guid.NewGuid(),
            EnterpriseName = "Acme Corp",
            EnterpriseCode = "ACM",
            SubscriptionPlanId = Guid.NewGuid(),
            IsDeleted = false
        };

        var report = new Report
        {
            Id = Guid.NewGuid(),
            ReportedById = reporter.Id,
            ReportedBy = reporter,
            EntityType = ReportConstants.EntityType.Enterprise,
            EntityId = enterprise.Id,
            Reason = ReportConstants.Reason.Other,
            Status = ReportConstants.Status.Pending,
            CreatedAt = new DateTime(2026, 4, 2, 9, 0, 0, DateTimeKind.Utc)
        };

        _mockContext.Setup(x => x.Reports).Returns(new List<Report> { report }.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.JobPostings).Returns(new List<JobPosting>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Enterprises).Returns(new List<Enterprise> { enterprise }.AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetReportByIdQuery { Id = report.Id }, CancellationToken.None);

        result.EntityName.Should().Be(enterprise.EnterpriseName);
        result.EntityType.Should().Be(ReportConstants.EntityType.Enterprise);
    }

    [Fact]
    public async Task Handle_ShouldThrowKeyNotFound_WhenReportDoesNotExist()
    {
        _mockContext.Setup(x => x.Reports).Returns(new List<Report>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.JobPostings).Returns(new List<JobPosting>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Enterprises).Returns(new List<Enterprise>().AsQueryable().BuildMockDbSet().Object);

        var act = async () => await _handler.Handle(new GetReportByIdQuery { Id = Guid.NewGuid() }, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}

