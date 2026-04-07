using ERMS.Application.Features.Reports.Queries.GetReportStats;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.System;
using ERMS.Domain.Entities.System;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace ERMS.UnitTests.Features.Reports.Queries.GetReportStats;

public class GetReportStatsHandlerTests
{
    private readonly Mock<IERMSDbContext> _mockContext;
    private readonly GetReportStatsHandler _handler;

    public GetReportStatsHandlerTests()
    {
        _mockContext = new Mock<IERMSDbContext>();
        _handler = new GetReportStatsHandler(_mockContext.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnAllCounts()
    {
        _mockContext.Setup(x => x.Reports).Returns(new List<Report>
        {
            CreateReport(ReportConstants.Status.Pending, ReportConstants.EntityType.JobPosting),
            CreateReport(ReportConstants.Status.Pending, ReportConstants.EntityType.Enterprise),
            CreateReport(ReportConstants.Status.Reviewing, ReportConstants.EntityType.JobPosting),
            CreateReport(ReportConstants.Status.Resolved, ReportConstants.EntityType.JobPosting),
            CreateReport(ReportConstants.Status.Dismissed, ReportConstants.EntityType.Enterprise)
        }.AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetReportStatsQuery(), CancellationToken.None);

        result.TotalPending.Should().Be(2);
        result.TotalReviewing.Should().Be(1);
        result.TotalResolved.Should().Be(1);
        result.TotalDismissed.Should().Be(1);
        result.TotalJobPostingReports.Should().Be(3);
        result.TotalEnterpriseReports.Should().Be(2);
    }

    private static Report CreateReport(string status, string entityType)
    {
        return new Report
        {
            Id = Guid.NewGuid(),
            ReportedById = Guid.NewGuid(),
            EntityId = Guid.NewGuid(),
            EntityType = entityType,
            Reason = ReportConstants.Reason.Other,
            Status = status
        };
    }
}

