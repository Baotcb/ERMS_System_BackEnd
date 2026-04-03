using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERMS.Application.Features.Admin.Queries.GetAiServiceOverview;
using ERMS.Application.Interface;
using CVScreeningResultEntity = ERMS.Domain.Entities.Application.CVScreeningResult;
using ApplicationEntity = ERMS.Domain.Entities.Application.Application;
using ERMS.Domain.Entities.Enterprise;
using ERMS.Domain.Entities.Recruitment;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace ERMS.UnitTests.Features.Admin.Queries.GetAiServiceOverview;

public class GetAiServiceOverviewHandlerTests
{
    private readonly Mock<IERMSDbContext> _mockContext;
    private readonly Mock<IAIServiceConfiguration> _mockConfiguration;
    private readonly GetAiServiceOverviewHandler _handler;

    public GetAiServiceOverviewHandlerTests()
    {
        _mockContext = new Mock<IERMSDbContext>();
        _mockConfiguration = new Mock<IAIServiceConfiguration>();
        _handler = new GetAiServiceOverviewHandler(_mockContext.Object, _mockConfiguration.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnGeminiConfiguration_AndUsageStatistics()
    {
        var now = DateTime.UtcNow;
        var enterpriseId = Guid.NewGuid();

        var results = new List<CVScreeningResultEntity>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OverallScore = 82,
                ProcessedAt = now.AddHours(-1),
                AIModel = "gemini-2.5-flash",
                Application = new ApplicationEntity
                {
                    JobPosting = new JobPosting { EnterpriseId = enterpriseId }
                }
            },
            new()
            {
                Id = Guid.NewGuid(),
                OverallScore = 58,
                ProcessedAt = now.AddDays(-2),
                AIModel = "gemini-2.5-flash",
                Application = new ApplicationEntity
                {
                    JobPosting = new JobPosting { EnterpriseId = Guid.NewGuid() }
                }
            },
            new()
            {
                Id = Guid.NewGuid(),
                OverallScore = 35,
                ProcessedAt = now.AddDays(-6),
                AIModel = "gemini-2.5-flash",
                Application = new ApplicationEntity
                {
                    JobPosting = new JobPosting { EnterpriseId = enterpriseId }
                }
            }
        };

        _mockConfiguration.SetupGet(x => x.ProviderName).Returns("Gemini");
        _mockConfiguration.SetupGet(x => x.ModelName).Returns("gemini-2.5-flash");
        _mockConfiguration.SetupGet(x => x.HasApiKey).Returns(true);
        _mockContext.Setup(x => x.CVScreeningResults).Returns(results.AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetAiServiceOverviewQuery(), CancellationToken.None);

        result.ProviderName.Should().Be("Gemini");
        result.ModelName.Should().Be("gemini-2.5-flash");
        result.ApiKeyConfigured.Should().BeTrue();
        result.ScoredToday.Should().Be(1);
        result.ScoredLast7Days.Should().Be(3);
        result.ScoredLast30Days.Should().Be(3);
        result.DistinctEnterprisesLast30Days.Should().Be(2);
        result.LastProcessedAt.Should().BeCloseTo(now.AddHours(-1), TimeSpan.FromSeconds(1));
        result.ScoreDistribution.Should().ContainSingle(item => item.Bucket == "71-100" && item.Count == 1);
        result.ScoreDistribution.Should().ContainSingle(item => item.Bucket == "41-70" && item.Count == 1);
        result.ScoreDistribution.Should().ContainSingle(item => item.Bucket == "0-40" && item.Count == 1);
        result.DailyVolumes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnZeroedStatistics_WhenThereAreNoResults()
    {
        _mockConfiguration.SetupGet(x => x.ProviderName).Returns("Gemini");
        _mockConfiguration.SetupGet(x => x.ModelName).Returns("gemini-2.5-flash");
        _mockConfiguration.SetupGet(x => x.HasApiKey).Returns(false);
        _mockContext.Setup(x => x.CVScreeningResults).Returns(new List<CVScreeningResultEntity>().AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetAiServiceOverviewQuery(), CancellationToken.None);

        result.ApiKeyConfigured.Should().BeFalse();
        result.ConfigurationStatus.Should().Be("Missing configuration");
        result.ScoredToday.Should().Be(0);
        result.ScoredLast7Days.Should().Be(0);
        result.ScoredLast30Days.Should().Be(0);
        result.DistinctEnterprisesLast30Days.Should().Be(0);
        result.AverageScoreLast30Days.Should().Be(0);
        result.LastProcessedAt.Should().BeNull();
        result.DailyVolumes.Should().HaveCount(7);
        result.DailyVolumes.Should().OnlyContain(item => item.Count == 0);
        result.ScoreDistribution.Should().OnlyContain(item => item.Count == 0);
    }

    [Fact]
    public async Task Handle_ShouldIgnoreResultsOlderThanThirtyDays()
    {
        var now = DateTime.UtcNow;
        var results = new List<CVScreeningResultEntity>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OverallScore = 90,
                ProcessedAt = now.AddDays(-5),
                AIModel = "gemini-2.5-flash",
                Application = new ApplicationEntity
                {
                    JobPosting = new JobPosting { EnterpriseId = Guid.NewGuid() }
                }
            },
            new()
            {
                Id = Guid.NewGuid(),
                OverallScore = 20,
                ProcessedAt = now.AddDays(-35),
                AIModel = "gemini-2.5-flash",
                Application = new ApplicationEntity
                {
                    JobPosting = new JobPosting { EnterpriseId = Guid.NewGuid() }
                }
            }
        };

        _mockConfiguration.SetupGet(x => x.ProviderName).Returns("Gemini");
        _mockConfiguration.SetupGet(x => x.ModelName).Returns("gemini-2.5-flash");
        _mockConfiguration.SetupGet(x => x.HasApiKey).Returns(true);
        _mockContext.Setup(x => x.CVScreeningResults).Returns(results.AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetAiServiceOverviewQuery(), CancellationToken.None);

        result.ScoredLast30Days.Should().Be(1);
        result.AverageScoreLast30Days.Should().Be(90);
        result.ScoreDistribution.Should().ContainSingle(item => item.Bucket == "71-100" && item.Count == 1);
        result.ScoreDistribution.Should().ContainSingle(item => item.Bucket == "0-40" && item.Count == 0);
    }

    [Fact]
    public async Task Handle_ShouldRoundAverageScoreToOneDecimalPlace()
    {
        var now = DateTime.UtcNow;
        var enterpriseId = Guid.NewGuid();
        var results = new List<CVScreeningResultEntity>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OverallScore = 50,
                ProcessedAt = now.AddDays(-1),
                Application = new ApplicationEntity
                {
                    JobPosting = new JobPosting { EnterpriseId = enterpriseId }
                }
            },
            new()
            {
                Id = Guid.NewGuid(),
                OverallScore = 51,
                ProcessedAt = now.AddDays(-2),
                Application = new ApplicationEntity
                {
                    JobPosting = new JobPosting { EnterpriseId = enterpriseId }
                }
            },
            new()
            {
                Id = Guid.NewGuid(),
                OverallScore = 66,
                ProcessedAt = now.AddDays(-3),
                Application = new ApplicationEntity
                {
                    JobPosting = new JobPosting { EnterpriseId = enterpriseId }
                }
            }
        };

        _mockConfiguration.SetupGet(x => x.ProviderName).Returns("Gemini");
        _mockConfiguration.SetupGet(x => x.ModelName).Returns("gemini-2.5-flash");
        _mockConfiguration.SetupGet(x => x.HasApiKey).Returns(true);
        _mockContext.Setup(x => x.CVScreeningResults).Returns(results.AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetAiServiceOverviewQuery(), CancellationToken.None);

        result.AverageScoreLast30Days.Should().Be(55.7m);
    }

    [Fact]
    public async Task Handle_ShouldBucketBoundaryScoresCorrectly()
    {
        var now = DateTime.UtcNow;
        var results = new List<CVScreeningResultEntity>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OverallScore = 40,
                ProcessedAt = now.AddDays(-1),
                Application = new ApplicationEntity { JobPosting = new JobPosting { EnterpriseId = Guid.NewGuid() } }
            },
            new()
            {
                Id = Guid.NewGuid(),
                OverallScore = 41,
                ProcessedAt = now.AddDays(-1),
                Application = new ApplicationEntity { JobPosting = new JobPosting { EnterpriseId = Guid.NewGuid() } }
            },
            new()
            {
                Id = Guid.NewGuid(),
                OverallScore = 70,
                ProcessedAt = now.AddDays(-1),
                Application = new ApplicationEntity { JobPosting = new JobPosting { EnterpriseId = Guid.NewGuid() } }
            },
            new()
            {
                Id = Guid.NewGuid(),
                OverallScore = 71,
                ProcessedAt = now.AddDays(-1),
                Application = new ApplicationEntity { JobPosting = new JobPosting { EnterpriseId = Guid.NewGuid() } }
            }
        };

        _mockConfiguration.SetupGet(x => x.ProviderName).Returns("Gemini");
        _mockConfiguration.SetupGet(x => x.ModelName).Returns("gemini-2.5-flash");
        _mockConfiguration.SetupGet(x => x.HasApiKey).Returns(true);
        _mockContext.Setup(x => x.CVScreeningResults).Returns(results.AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetAiServiceOverviewQuery(), CancellationToken.None);

        result.ScoreDistribution.Should().ContainSingle(item => item.Bucket == "0-40" && item.Count == 1);
        result.ScoreDistribution.Should().ContainSingle(item => item.Bucket == "41-70" && item.Count == 2);
        result.ScoreDistribution.Should().ContainSingle(item => item.Bucket == "71-100" && item.Count == 1);
    }

    [Fact]
    public async Task Handle_ShouldCountDistinctEnterprisesOnlyOnce_WhenMultipleResultsBelongToSameEnterprise()
    {
        var now = DateTime.UtcNow;
        var sharedEnterpriseId = Guid.NewGuid();
        var results = new List<CVScreeningResultEntity>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OverallScore = 60,
                ProcessedAt = now.AddDays(-1),
                Application = new ApplicationEntity { JobPosting = new JobPosting { EnterpriseId = sharedEnterpriseId } }
            },
            new()
            {
                Id = Guid.NewGuid(),
                OverallScore = 61,
                ProcessedAt = now.AddDays(-2),
                Application = new ApplicationEntity { JobPosting = new JobPosting { EnterpriseId = sharedEnterpriseId } }
            },
            new()
            {
                Id = Guid.NewGuid(),
                OverallScore = 62,
                ProcessedAt = now.AddDays(-3),
                Application = new ApplicationEntity { JobPosting = new JobPosting { EnterpriseId = Guid.NewGuid() } }
            }
        };

        _mockConfiguration.SetupGet(x => x.ProviderName).Returns("Gemini");
        _mockConfiguration.SetupGet(x => x.ModelName).Returns("gemini-2.5-flash");
        _mockConfiguration.SetupGet(x => x.HasApiKey).Returns(true);
        _mockContext.Setup(x => x.CVScreeningResults).Returns(results.AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetAiServiceOverviewQuery(), CancellationToken.None);

        result.DistinctEnterprisesLast30Days.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ShouldBuildSevenDayDailyVolumes_InChronologicalOrder()
    {
        var today = DateTime.UtcNow.Date;
        var results = new List<CVScreeningResultEntity>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OverallScore = 80,
                ProcessedAt = today.AddDays(-6).AddHours(8),
                Application = new ApplicationEntity { JobPosting = new JobPosting { EnterpriseId = Guid.NewGuid() } }
            },
            new()
            {
                Id = Guid.NewGuid(),
                OverallScore = 81,
                ProcessedAt = today.AddDays(-3).AddHours(9),
                Application = new ApplicationEntity { JobPosting = new JobPosting { EnterpriseId = Guid.NewGuid() } }
            },
            new()
            {
                Id = Guid.NewGuid(),
                OverallScore = 82,
                ProcessedAt = today.AddHours(10),
                Application = new ApplicationEntity { JobPosting = new JobPosting { EnterpriseId = Guid.NewGuid() } }
            }
        };

        _mockConfiguration.SetupGet(x => x.ProviderName).Returns("Gemini");
        _mockConfiguration.SetupGet(x => x.ModelName).Returns("gemini-2.5-flash");
        _mockConfiguration.SetupGet(x => x.HasApiKey).Returns(true);
        _mockContext.Setup(x => x.CVScreeningResults).Returns(results.AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetAiServiceOverviewQuery(), CancellationToken.None);

        result.DailyVolumes.Should().HaveCount(7);
        result.DailyVolumes.Select(item => item.Date).Should().BeInAscendingOrder();
        result.DailyVolumes.Should().Contain(item => item.Date.Date == today.AddDays(-6) && item.Count == 1);
        result.DailyVolumes.Should().Contain(item => item.Date.Date == today.AddDays(-3) && item.Count == 1);
        result.DailyVolumes.Should().Contain(item => item.Date.Date == today && item.Count == 1);
    }

    [Fact]
    public async Task Handle_ShouldReportConfiguredStatus_WhenApiKeyExistsWithoutResults()
    {
        _mockConfiguration.SetupGet(x => x.ProviderName).Returns("Gemini");
        _mockConfiguration.SetupGet(x => x.ModelName).Returns("gemini-2.5-flash");
        _mockConfiguration.SetupGet(x => x.HasApiKey).Returns(true);
        _mockContext.Setup(x => x.CVScreeningResults).Returns(new List<CVScreeningResultEntity>().AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetAiServiceOverviewQuery(), CancellationToken.None);

        result.ApiKeyConfigured.Should().BeTrue();
        result.ConfigurationStatus.Should().Be("Configured");
        result.ScoredLast30Days.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldTreatTodayAndSevenDayWindowBoundariesAsInclusive()
    {
        var now = DateTime.UtcNow;
        var startOfToday = now.Date;
        var startOf7Days = startOfToday.AddDays(-6);
        var results = new List<CVScreeningResultEntity>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OverallScore = 75,
                ProcessedAt = startOfToday,
                Application = new ApplicationEntity
                {
                    JobPosting = new JobPosting { EnterpriseId = Guid.NewGuid() }
                }
            },
            new()
            {
                Id = Guid.NewGuid(),
                OverallScore = 65,
                ProcessedAt = startOf7Days,
                Application = new ApplicationEntity
                {
                    JobPosting = new JobPosting { EnterpriseId = Guid.NewGuid() }
                }
            },
            new()
            {
                Id = Guid.NewGuid(),
                OverallScore = 55,
                ProcessedAt = startOf7Days.AddDays(-1),
                Application = new ApplicationEntity
                {
                    JobPosting = new JobPosting { EnterpriseId = Guid.NewGuid() }
                }
            }
        };

        _mockConfiguration.SetupGet(x => x.ProviderName).Returns("Gemini");
        _mockConfiguration.SetupGet(x => x.ModelName).Returns("gemini-2.5-flash");
        _mockConfiguration.SetupGet(x => x.HasApiKey).Returns(true);
        _mockContext.Setup(x => x.CVScreeningResults).Returns(results.AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetAiServiceOverviewQuery(), CancellationToken.None);

        result.ScoredToday.Should().Be(1);
        result.ScoredLast7Days.Should().Be(2);
        result.ScoredLast30Days.Should().Be(3);
    }

    [Fact]
    public async Task Handle_ShouldReturnMostRecentProcessedTimestamp()
    {
        var now = DateTime.UtcNow;
        var latestProcessedAt = now.AddHours(-1);
        var results = new List<CVScreeningResultEntity>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OverallScore = 80,
                ProcessedAt = now.AddDays(-2),
                Application = new ApplicationEntity
                {
                    JobPosting = new JobPosting { EnterpriseId = Guid.NewGuid() }
                }
            },
            new()
            {
                Id = Guid.NewGuid(),
                OverallScore = 81,
                ProcessedAt = latestProcessedAt,
                Application = new ApplicationEntity
                {
                    JobPosting = new JobPosting { EnterpriseId = Guid.NewGuid() }
                }
            }
        };

        _mockConfiguration.SetupGet(x => x.ProviderName).Returns("Gemini");
        _mockConfiguration.SetupGet(x => x.ModelName).Returns("gemini-2.5-flash");
        _mockConfiguration.SetupGet(x => x.HasApiKey).Returns(true);
        _mockContext.Setup(x => x.CVScreeningResults).Returns(results.AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetAiServiceOverviewQuery(), CancellationToken.None);

        result.LastProcessedAt.Should().BeCloseTo(latestProcessedAt, TimeSpan.FromSeconds(1));
    }
}
