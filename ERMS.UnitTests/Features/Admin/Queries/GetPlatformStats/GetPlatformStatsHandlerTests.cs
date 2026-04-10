using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERMS.Application.Features.Admin.Queries.GetPlatformStats;
using ERMS.Application.Features.Admin.Queries.GetSystemIntegrations;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Enterprise;
using ERMS.Domain.Entities.Enterprise;
using ERMS.Domain.Entities.Organization;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace ERMS.UnitTests.Features.Admin.Queries.GetPlatformStats;

public class GetPlatformStatsHandlerTests
{
    private readonly Mock<IERMSDbContext> _mockContext;
    private readonly Mock<ISystemIntegrationStatusService> _mockIntegrationService;
    private readonly GetPlatformStatsHandler _handler;

    public GetPlatformStatsHandlerTests()
    {
        _mockContext = new Mock<IERMSDbContext>();
        _mockIntegrationService = new Mock<ISystemIntegrationStatusService>();
        _mockIntegrationService
            .Setup(x => x.GetSystemIntegrationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetSystemIntegrationsResponse());
        _handler = new GetPlatformStatsHandler(_mockContext.Object, _mockIntegrationService.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnBusinessKpis_AndPlatformBreakdowns()
    {
        var now = DateTime.UtcNow;
        var freePlan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Basic",
            PlanCode = "BASIC",
            PriceMonthly = 0
        };
        var proPlan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Pro",
            PlanCode = "PRO",
            PriceMonthly = 2000000
        };
        var secondaryFreePlan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Starter",
            PlanCode = "FREE",
            PriceMonthly = 5000000
        };

        var expiringSoonId = Guid.NewGuid();
        var topEnterpriseId = Guid.NewGuid();
        var enterprises = new List<Enterprise>
        {
            new()
            {
                Id = topEnterpriseId,
                EnterpriseName = "ABC Tech",
                Status = EnterpriseStatus.Active,
                SubscriptionPlan = proPlan,
                SubscriptionPlanId = proPlan.Id,
                SubscriptionEndDate = now.AddDays(60),
                IsDeleted = false
            },
            new()
            {
                Id = expiringSoonId,
                EnterpriseName = "Renewal Soon Co",
                Status = EnterpriseStatus.Active,
                SubscriptionPlan = freePlan,
                SubscriptionPlanId = freePlan.Id,
                SubscriptionEndDate = now.AddDays(12),
                IsDeleted = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseName = "Locked Pro Co",
                Status = EnterpriseStatus.Locked,
                SubscriptionPlan = secondaryFreePlan,
                SubscriptionPlanId = secondaryFreePlan.Id,
                SubscriptionEndDate = now.AddDays(35),
                IsDeleted = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseName = "Suspended Free Co",
                Status = EnterpriseStatus.Suspended,
                SubscriptionPlan = freePlan,
                SubscriptionPlanId = freePlan.Id,
                SubscriptionEndDate = now.AddDays(80),
                IsDeleted = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseName = "Inactive Pro Co",
                Status = EnterpriseStatus.Inactive,
                SubscriptionPlan = proPlan,
                SubscriptionPlanId = proPlan.Id,
                SubscriptionEndDate = now.AddDays(-3),
                IsDeleted = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseName = "Deleted Locked Co",
                Status = EnterpriseStatus.Locked,
                SubscriptionPlan = proPlan,
                SubscriptionPlanId = proPlan.Id,
                SubscriptionEndDate = now.AddDays(10),
                IsDeleted = true
            }
        };

        var employees = new List<Employee>
        {
            new() { Id = Guid.NewGuid(), EnterpriseId = topEnterpriseId, EmployeeCode = "EMP001", UserId = Guid.NewGuid(), IsDeleted = false },
            new() { Id = Guid.NewGuid(), EnterpriseId = topEnterpriseId, EmployeeCode = "EMP002", UserId = Guid.NewGuid(), IsDeleted = false },
            new() { Id = Guid.NewGuid(), EnterpriseId = topEnterpriseId, EmployeeCode = "EMP003", UserId = Guid.NewGuid(), IsDeleted = false },
            new() { Id = Guid.NewGuid(), EnterpriseId = expiringSoonId, EmployeeCode = "EMP004", UserId = Guid.NewGuid(), IsDeleted = false }
        };

        var histories = new List<SubscriptionHistory>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseId = topEnterpriseId,
                Enterprise = enterprises[0],
                SubscriptionPlanId = proPlan.Id,
                SubscriptionPlan = proPlan,
                ActionType = "Renew",
                PeriodStartDate = now.AddMonths(-1),
                PeriodEndDate = now.AddDays(-4),
                CreatedAt = now.AddDays(-3)
            },
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseId = expiringSoonId,
                Enterprise = enterprises[1],
                SubscriptionPlanId = freePlan.Id,
                SubscriptionPlan = freePlan,
                ActionType = "Subscribe",
                PeriodStartDate = now.AddMonths(-1),
                PeriodEndDate = now.AddDays(-2),
                CreatedAt = now.AddDays(-40)
            }
        };

        _mockContext.Setup(x => x.Enterprises).Returns(enterprises.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(employees.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.SubscriptionHistories).Returns(histories.AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetPlatformStatsQuery(), CancellationToken.None);

        result.TotalEnterprises.Should().Be(5);
        result.ActiveEnterprises.Should().Be(2);
        result.LockedEnterprises.Should().Be(1);
        result.SuspendedEnterprises.Should().Be(1);
        result.InactiveEnterprises.Should().Be(1);
        result.MrrCurrentMonth.Should().Be(2000000);
        result.RenewalRate.Should().Be(50);
        result.SubscriptionMix.Should().ContainSingle(item => item.TierName == "Free" && item.Count == 3);
        result.SubscriptionMix.Should().ContainSingle(item => item.TierName == "Pro" && item.Count == 2);
        result.StatusDistribution.Should().ContainSingle(item => item.Status == EnterpriseStatus.Active && item.Count == 2);
        result.TopEnterprises.Should().ContainSingle(item => item.EnterpriseId == topEnterpriseId && item.Value == 3);
        result.ChurnWatchlist.Should().Contain(item => item.EnterpriseId == expiringSoonId);
        result.ChurnWatchlist.Should().Contain(item => item.Status == EnterpriseStatus.Locked);
    }

    [Fact]
    public async Task Handle_ShouldReturnZeroRenewalRate_WhenNoEnterprisesAreDueForRenewal()
    {
        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Pro",
            PlanCode = "PRO",
            PriceMonthly = 2000000
        };

        var enterprises = new List<Enterprise>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseName = "Active Pro",
                Status = EnterpriseStatus.Active,
                SubscriptionPlan = plan,
                SubscriptionPlanId = plan.Id,
                SubscriptionEndDate = DateTime.UtcNow.AddDays(45),
                IsDeleted = false
            }
        };

        _mockContext.Setup(x => x.Enterprises).Returns(enterprises.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(new List<Employee>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.SubscriptionHistories).Returns(new List<SubscriptionHistory>().AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetPlatformStatsQuery(), CancellationToken.None);

        result.RenewalRate.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldCountOnlyActiveAndNonExpiredProPlans_InMrr()
    {
        var now = DateTime.UtcNow;
        var proPlan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Pro",
            PlanCode = "PRO",
            PriceMonthly = 3000000
        };
        var freePlan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Starter",
            PlanCode = "FREE",
            PriceMonthly = 5000000
        };

        var enterprises = new List<Enterprise>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseName = "Eligible Pro",
                Status = EnterpriseStatus.Active,
                SubscriptionPlan = proPlan,
                SubscriptionPlanId = proPlan.Id,
                SubscriptionEndDate = now.AddDays(20),
                IsDeleted = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseName = "Expired Pro",
                Status = EnterpriseStatus.Active,
                SubscriptionPlan = proPlan,
                SubscriptionPlanId = proPlan.Id,
                SubscriptionEndDate = now.AddDays(-1),
                IsDeleted = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseName = "Inactive Growth",
                Status = EnterpriseStatus.Inactive,
                SubscriptionPlan = freePlan,
                SubscriptionPlanId = freePlan.Id,
                SubscriptionEndDate = now.AddDays(10),
                IsDeleted = false
            }
        };

        _mockContext.Setup(x => x.Enterprises).Returns(enterprises.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(new List<Employee>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.SubscriptionHistories).Returns(new List<SubscriptionHistory>().AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetPlatformStatsQuery(), CancellationToken.None);

        result.MrrCurrentMonth.Should().Be(3000000);
        result.SubscriptionMix.Should().ContainSingle(item => item.TierName == "Pro" && item.Count == 2);
    }

    [Fact]
    public async Task Handle_ShouldUseDistinctEnterpriseIds_WhenCalculatingRenewalRate()
    {
        var now = DateTime.UtcNow;
        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Pro",
            PlanCode = "PRO",
            PriceMonthly = 1500000
        };

        var renewedEnterpriseId = Guid.NewGuid();
        var notRenewedEnterpriseId = Guid.NewGuid();
        var enterprises = new List<Enterprise>
        {
            new()
            {
                Id = renewedEnterpriseId,
                EnterpriseName = "Renewed Co",
                Status = EnterpriseStatus.Active,
                SubscriptionPlan = plan,
                SubscriptionPlanId = plan.Id,
                SubscriptionEndDate = now.AddDays(20),
                IsDeleted = false
            },
            new()
            {
                Id = notRenewedEnterpriseId,
                EnterpriseName = "Due Co",
                Status = EnterpriseStatus.Active,
                SubscriptionPlan = plan,
                SubscriptionPlanId = plan.Id,
                SubscriptionEndDate = now.AddDays(20),
                IsDeleted = false
            }
        };

        var histories = new List<SubscriptionHistory>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseId = renewedEnterpriseId,
                Enterprise = enterprises[0],
                SubscriptionPlanId = plan.Id,
                SubscriptionPlan = plan,
                ActionType = "Renew",
                PeriodEndDate = now.AddDays(-2),
                CreatedAt = now.AddDays(-1)
            },
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseId = renewedEnterpriseId,
                Enterprise = enterprises[0],
                SubscriptionPlanId = plan.Id,
                SubscriptionPlan = plan,
                ActionType = "Renew",
                PeriodEndDate = now.AddDays(-2),
                CreatedAt = now.AddHours(-12)
            },
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseId = notRenewedEnterpriseId,
                Enterprise = enterprises[1],
                SubscriptionPlanId = plan.Id,
                SubscriptionPlan = plan,
                ActionType = "Subscribe",
                PeriodEndDate = now.AddDays(-3),
                CreatedAt = now.AddDays(-20)
            }
        };

        _mockContext.Setup(x => x.Enterprises).Returns(enterprises.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(new List<Employee>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.SubscriptionHistories).Returns(histories.AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetPlatformStatsQuery(), CancellationToken.None);

        result.RenewalRate.Should().Be(50);
    }

    [Fact]
    public async Task Handle_ShouldOrderTopEnterprises_ByEmployeeCountThenName_AndIgnoreDeletedEmployees()
    {
        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Pro",
            PlanCode = "PRO",
            PriceMonthly = 1000000
        };

        var alphaId = Guid.NewGuid();
        var betaId = Guid.NewGuid();
        var enterprises = new List<Enterprise>
        {
            new() { Id = betaId, EnterpriseName = "Beta Co", Status = EnterpriseStatus.Active, SubscriptionPlan = plan, SubscriptionPlanId = plan.Id, SubscriptionEndDate = DateTime.UtcNow.AddDays(20), IsDeleted = false },
            new() { Id = alphaId, EnterpriseName = "Alpha Co", Status = EnterpriseStatus.Active, SubscriptionPlan = plan, SubscriptionPlanId = plan.Id, SubscriptionEndDate = DateTime.UtcNow.AddDays(20), IsDeleted = false },
            new() { Id = Guid.NewGuid(), EnterpriseName = "Gamma Co", Status = EnterpriseStatus.Active, SubscriptionPlan = plan, SubscriptionPlanId = plan.Id, SubscriptionEndDate = DateTime.UtcNow.AddDays(20), IsDeleted = false }
        };

        var employees = new List<Employee>
        {
            new() { Id = Guid.NewGuid(), EnterpriseId = alphaId, EmployeeCode = "A1", UserId = Guid.NewGuid(), IsDeleted = false },
            new() { Id = Guid.NewGuid(), EnterpriseId = alphaId, EmployeeCode = "A2", UserId = Guid.NewGuid(), IsDeleted = false },
            new() { Id = Guid.NewGuid(), EnterpriseId = alphaId, EmployeeCode = "A3", UserId = Guid.NewGuid(), IsDeleted = true },
            new() { Id = Guid.NewGuid(), EnterpriseId = betaId, EmployeeCode = "B1", UserId = Guid.NewGuid(), IsDeleted = false },
            new() { Id = Guid.NewGuid(), EnterpriseId = betaId, EmployeeCode = "B2", UserId = Guid.NewGuid(), IsDeleted = false },
            new() { Id = Guid.NewGuid(), EnterpriseId = enterprises[2].Id, EmployeeCode = "G1", UserId = Guid.NewGuid(), IsDeleted = false }
        };

        _mockContext.Setup(x => x.Enterprises).Returns(enterprises.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(employees.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.SubscriptionHistories).Returns(new List<SubscriptionHistory>().AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetPlatformStatsQuery(), CancellationToken.None);

        result.TopEnterprises.Should().HaveCount(3);
        result.TopEnterprises[0].EnterpriseName.Should().Be("Alpha Co");
        result.TopEnterprises[0].Value.Should().Be(2);
        result.TopEnterprises[1].EnterpriseName.Should().Be("Beta Co");
        result.TopEnterprises[1].Value.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ShouldLimitChurnWatchlistToFive_AndOrderByPriorityThenExpiry()
    {
        var now = DateTime.UtcNow;
        var plan = new SubscriptionPlan { Id = Guid.NewGuid(), PlanName = "Pro", PlanCode = "PRO", PriceMonthly = 1000000 };
        var enterprises = new List<Enterprise>
        {
            new() { Id = Guid.NewGuid(), EnterpriseName = "Locked Co", Status = EnterpriseStatus.Locked, SubscriptionPlan = plan, SubscriptionPlanId = plan.Id, SubscriptionEndDate = now.AddDays(50), IsDeleted = false },
            new() { Id = Guid.NewGuid(), EnterpriseName = "Inactive Co", Status = EnterpriseStatus.Inactive, SubscriptionPlan = plan, SubscriptionPlanId = plan.Id, SubscriptionEndDate = now.AddDays(40), IsDeleted = false },
            new() { Id = Guid.NewGuid(), EnterpriseName = "Suspended Co", Status = EnterpriseStatus.Suspended, SubscriptionPlan = plan, SubscriptionPlanId = plan.Id, SubscriptionEndDate = now.AddDays(30), IsDeleted = false },
            new() { Id = Guid.NewGuid(), EnterpriseName = "Expired Co", Status = EnterpriseStatus.Active, SubscriptionPlan = plan, SubscriptionPlanId = plan.Id, SubscriptionEndDate = now.AddDays(-2), IsDeleted = false },
            new() { Id = Guid.NewGuid(), EnterpriseName = "Expiring 4", Status = EnterpriseStatus.Active, SubscriptionPlan = plan, SubscriptionPlanId = plan.Id, SubscriptionEndDate = now.AddDays(4), IsDeleted = false },
            new() { Id = Guid.NewGuid(), EnterpriseName = "Expiring 20", Status = EnterpriseStatus.Active, SubscriptionPlan = plan, SubscriptionPlanId = plan.Id, SubscriptionEndDate = now.AddDays(20), IsDeleted = false }
        };

        _mockContext.Setup(x => x.Enterprises).Returns(enterprises.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(new List<Employee>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.SubscriptionHistories).Returns(new List<SubscriptionHistory>().AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetPlatformStatsQuery(), CancellationToken.None);

        result.ChurnWatchlist.Should().HaveCount(5);
        result.ChurnWatchlist.Select(item => item.EnterpriseName).Should().Equal(
            "Locked Co",
            "Inactive Co",
            "Suspended Co",
            "Expired Co",
            "Expiring 4");
    }

    [Fact]
    public async Task Handle_ShouldReturnAllStatusBuckets_EvenWhenCountIsZero()
    {
        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Free",
            PlanCode = "FREE",
            PriceMonthly = 0
        };

        var enterprises = new List<Enterprise>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseName = "Only Active",
                Status = EnterpriseStatus.Active,
                SubscriptionPlan = plan,
                SubscriptionPlanId = plan.Id,
                SubscriptionEndDate = DateTime.UtcNow.AddDays(10),
                IsDeleted = false
            }
        };

        _mockContext.Setup(x => x.Enterprises).Returns(enterprises.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(new List<Employee>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.SubscriptionHistories).Returns(new List<SubscriptionHistory>().AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetPlatformStatsQuery(), CancellationToken.None);

        result.StatusDistribution.Should().HaveCount(4);
        result.StatusDistribution.Should().Contain(item => item.Status == EnterpriseStatus.Active && item.Count == 1);
        result.StatusDistribution.Should().Contain(item => item.Status == EnterpriseStatus.Locked && item.Count == 0);
        result.StatusDistribution.Should().Contain(item => item.Status == EnterpriseStatus.Suspended && item.Count == 0);
        result.StatusDistribution.Should().Contain(item => item.Status == EnterpriseStatus.Inactive && item.Count == 0);
    }

    [Fact]
    public async Task Handle_ShouldIncludeRemainingDaysInExpiringRiskReason()
    {
        var now = DateTime.UtcNow;
        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Pro",
            PlanCode = "PRO",
            PriceMonthly = 1000000
        };
        var enterpriseId = Guid.NewGuid();
        var enterprises = new List<Enterprise>
        {
            new()
            {
                Id = enterpriseId,
                EnterpriseName = "Expiring Soon",
                Status = EnterpriseStatus.Active,
                SubscriptionPlan = plan,
                SubscriptionPlanId = plan.Id,
                SubscriptionEndDate = now.AddDays(7),
                IsDeleted = false
            }
        };

        _mockContext.Setup(x => x.Enterprises).Returns(enterprises.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(new List<Employee>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.SubscriptionHistories).Returns(new List<SubscriptionHistory>().AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetPlatformStatsQuery(), CancellationToken.None);

        result.ChurnWatchlist.Should().ContainSingle(item =>
            item.EnterpriseId == enterpriseId &&
            item.RiskReason.Contains("7 ngày"));
    }

    [Fact]
    public async Task Handle_ShouldLimitTopEnterprisesToFiveRows()
    {
        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Pro",
            PlanCode = "PRO",
            PriceMonthly = 1000000
        };
        var enterprises = Enumerable.Range(1, 6)
            .Select(index => new Enterprise
            {
                Id = Guid.NewGuid(),
                EnterpriseName = $"Enterprise {index}",
                Status = EnterpriseStatus.Active,
                SubscriptionPlan = plan,
                SubscriptionPlanId = plan.Id,
                SubscriptionEndDate = DateTime.UtcNow.AddDays(20),
                IsDeleted = false
            })
            .ToList();
        var employees = enterprises
            .SelectMany((enterprise, index) => Enumerable.Range(1, index + 1)
                .Select(employeeIndex => new Employee
                {
                    Id = Guid.NewGuid(),
                    EnterpriseId = enterprise.Id,
                    EmployeeCode = $"E{index + 1}{employeeIndex}",
                    UserId = Guid.NewGuid(),
                    IsDeleted = false
                }))
            .ToList();

        _mockContext.Setup(x => x.Enterprises).Returns(enterprises.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(employees.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.SubscriptionHistories).Returns(new List<SubscriptionHistory>().AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetPlatformStatsQuery(), CancellationToken.None);

        result.TopEnterprises.Should().HaveCount(5);
        result.TopEnterprises[0].EnterpriseName.Should().Be("Enterprise 6");
        result.TopEnterprises[^1].EnterpriseName.Should().Be("Enterprise 2");
    }

    [Fact]
    public async Task Handle_ShouldReturnZeroEmployeeValue_ForEnterprisesWithoutEmployees()
    {
        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Free",
            PlanCode = "FREE",
            PriceMonthly = 0
        };

        var enterprises = new List<Enterprise>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseName = "No Employees A",
                Status = EnterpriseStatus.Active,
                SubscriptionPlan = plan,
                SubscriptionPlanId = plan.Id,
                SubscriptionEndDate = DateTime.UtcNow.AddDays(20),
                IsDeleted = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseName = "No Employees B",
                Status = EnterpriseStatus.Active,
                SubscriptionPlan = plan,
                SubscriptionPlanId = plan.Id,
                SubscriptionEndDate = DateTime.UtcNow.AddDays(25),
                IsDeleted = false
            }
        };

        _mockContext.Setup(x => x.Enterprises).Returns(enterprises.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(new List<Employee>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.SubscriptionHistories).Returns(new List<SubscriptionHistory>().AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetPlatformStatsQuery(), CancellationToken.None);

        result.TopEnterprises.Should().HaveCount(2);
        result.TopEnterprises.Should().OnlyContain(item => item.Value == 0);
    }

    [Fact]
    public async Task Handle_ShouldReturnZeroIntegrationHealth_WhenNoIntegrationsExist()
    {
        var plan = new SubscriptionPlan { Id = Guid.NewGuid(), PlanName = "Free", PlanCode = "FREE", PriceMonthly = 0 };
        var enterprises = new List<Enterprise>
        {
            new() { Id = Guid.NewGuid(), EnterpriseName = "A", Status = EnterpriseStatus.Active, SubscriptionPlan = plan, SubscriptionPlanId = plan.Id, SubscriptionEndDate = DateTime.UtcNow.AddDays(30), IsDeleted = false }
        };

        _mockContext.Setup(x => x.Enterprises).Returns(enterprises.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(new List<Employee>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.SubscriptionHistories).Returns(new List<SubscriptionHistory>().AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetPlatformStatsQuery(), CancellationToken.None);

        result.IntegrationHealth.Should().NotBeNull();
        result.IntegrationHealth.Healthy.Should().Be(0);
        result.IntegrationHealth.Warning.Should().Be(0);
        result.IntegrationHealth.Error.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldClassifyIntegrationStatuses_IntoHealthWarningError()
    {
        var plan = new SubscriptionPlan { Id = Guid.NewGuid(), PlanName = "Free", PlanCode = "FREE", PriceMonthly = 0 };
        var enterprises = new List<Enterprise>
        {
            new() { Id = Guid.NewGuid(), EnterpriseName = "A", Status = EnterpriseStatus.Active, SubscriptionPlan = plan, SubscriptionPlanId = plan.Id, SubscriptionEndDate = DateTime.UtcNow.AddDays(30), IsDeleted = false }
        };

        var integrations = new GetSystemIntegrationsResponse
        {
            new SystemIntegrationDto { Name = "Email", Status = "Configured" },
            new SystemIntegrationDto { Name = "SMS", Status = "Configured" },
            new SystemIntegrationDto { Name = "Storage", Status = "MissingConfiguration" },
            new SystemIntegrationDto { Name = "Payment", Status = "Error" },
            new SystemIntegrationDto { Name = "AI", Status = "Unknown" }
        };

        _mockIntegrationService
            .Setup(x => x.GetSystemIntegrationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(integrations);

        _mockContext.Setup(x => x.Enterprises).Returns(enterprises.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(new List<Employee>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.SubscriptionHistories).Returns(new List<SubscriptionHistory>().AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetPlatformStatsQuery(), CancellationToken.None);

        result.IntegrationHealth.Healthy.Should().Be(2);
        result.IntegrationHealth.Warning.Should().Be(1);
        result.IntegrationHealth.Error.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ShouldReturnZeroedResponse_WhenThereAreNoEnterprises()
    {
        _mockContext.Setup(x => x.Enterprises).Returns(new List<Enterprise>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(new List<Employee>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.SubscriptionHistories).Returns(new List<SubscriptionHistory>().AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetPlatformStatsQuery(), CancellationToken.None);

        result.TotalEnterprises.Should().Be(0);
        result.ActiveEnterprises.Should().Be(0);
        result.LockedEnterprises.Should().Be(0);
        result.SuspendedEnterprises.Should().Be(0);
        result.InactiveEnterprises.Should().Be(0);
        result.MrrCurrentMonth.Should().Be(0);
        result.RenewalRate.Should().Be(0);
        result.StatusDistribution.Should().HaveCount(4);
        result.StatusDistribution.Should().OnlyContain(item => item.Count == 0);
        result.SubscriptionMix.Should().ContainSingle(item => item.TierName == "Free" && item.Count == 0);
        result.SubscriptionMix.Should().ContainSingle(item => item.TierName == "Pro" && item.Count == 0);
        result.TopEnterprises.Should().BeEmpty();
        result.ChurnWatchlist.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldClassifyProPlans_CaseInsensitively_AndIgnoreNullPlanFields()
    {
        var now = DateTime.UtcNow;
        var nameMatchedPlan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "PRO Max",
            PlanCode = null!,
            PriceMonthly = 2100000
        };
        var codeMatchedPlan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Starter",
            PlanCode = "team-pro-2026",
            PriceMonthly = 1800000
        };
        var nullPlan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = null!,
            PlanCode = null!,
            PriceMonthly = 9000000
        };

        var enterprises = new List<Enterprise>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseName = "Name Match Co",
                Status = EnterpriseStatus.Active,
                SubscriptionPlan = nameMatchedPlan,
                SubscriptionPlanId = nameMatchedPlan.Id,
                SubscriptionEndDate = now.AddDays(30),
                IsDeleted = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseName = "Code Match Co",
                Status = EnterpriseStatus.Active,
                SubscriptionPlan = codeMatchedPlan,
                SubscriptionPlanId = codeMatchedPlan.Id,
                SubscriptionEndDate = now.AddDays(45),
                IsDeleted = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseName = "Null Plan Co",
                Status = EnterpriseStatus.Active,
                SubscriptionPlan = nullPlan,
                SubscriptionPlanId = nullPlan.Id,
                SubscriptionEndDate = now.AddDays(60),
                IsDeleted = false
            }
        };

        _mockContext.Setup(x => x.Enterprises).Returns(enterprises.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(new List<Employee>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.SubscriptionHistories).Returns(new List<SubscriptionHistory>().AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetPlatformStatsQuery(), CancellationToken.None);

        result.SubscriptionMix.Should().ContainSingle(item => item.TierName == "Pro" && item.Count == 2);
        result.SubscriptionMix.Should().ContainSingle(item => item.TierName == "Free" && item.Count == 1);
        result.MrrCurrentMonth.Should().Be(3900000);
    }

    [Fact]
    public async Task Handle_ShouldIncludeOnlyEnterprises_ExpiringWithinThirtyDays_InChurnWatchlist()
    {
        var now = DateTime.UtcNow;
        var plan = new SubscriptionPlan { Id = Guid.NewGuid(), PlanName = "Pro", PlanCode = "PRO", PriceMonthly = 1000000 };
        var thresholdEnterpriseId = Guid.NewGuid();
        var outsideThresholdEnterpriseId = Guid.NewGuid();
        var enterprises = new List<Enterprise>
        {
            new()
            {
                Id = thresholdEnterpriseId,
                EnterpriseName = "Threshold Co",
                Status = EnterpriseStatus.Active,
                SubscriptionPlan = plan,
                SubscriptionPlanId = plan.Id,
                SubscriptionEndDate = now.AddDays(30),
                IsDeleted = false
            },
            new()
            {
                Id = outsideThresholdEnterpriseId,
                EnterpriseName = "Outside Threshold Co",
                Status = EnterpriseStatus.Active,
                SubscriptionPlan = plan,
                SubscriptionPlanId = plan.Id,
                SubscriptionEndDate = now.AddDays(31),
                IsDeleted = false
            }
        };

        _mockContext.Setup(x => x.Enterprises).Returns(enterprises.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(new List<Employee>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.SubscriptionHistories).Returns(new List<SubscriptionHistory>().AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetPlatformStatsQuery(), CancellationToken.None);

        result.ChurnWatchlist.Should().ContainSingle(item => item.EnterpriseId == thresholdEnterpriseId);
        result.ChurnWatchlist.Should().NotContain(item => item.EnterpriseId == outsideThresholdEnterpriseId);
    }

    [Fact]
    public async Task Handle_ShouldUseDistinctDueEnterpriseIds_WhenMultipleDueHistoriesBelongToSameEnterprise()
    {
        var now = DateTime.UtcNow;
        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Pro",
            PlanCode = "PRO",
            PriceMonthly = 1000000
        };
        var enterpriseId = Guid.NewGuid();
        var enterprises = new List<Enterprise>
        {
            new()
            {
                Id = enterpriseId,
                EnterpriseName = "Renewed Co",
                Status = EnterpriseStatus.Active,
                SubscriptionPlan = plan,
                SubscriptionPlanId = plan.Id,
                SubscriptionEndDate = now.AddDays(20),
                IsDeleted = false
            }
        };

        var histories = new List<SubscriptionHistory>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                Enterprise = enterprises[0],
                SubscriptionPlanId = plan.Id,
                SubscriptionPlan = plan,
                ActionType = "Subscribe",
                PeriodEndDate = now.AddDays(-8),
                CreatedAt = now.AddDays(-40)
            },
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                Enterprise = enterprises[0],
                SubscriptionPlanId = plan.Id,
                SubscriptionPlan = plan,
                ActionType = "Upgrade",
                PeriodEndDate = now.AddDays(-5),
                CreatedAt = now.AddDays(-20)
            },
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                Enterprise = enterprises[0],
                SubscriptionPlanId = plan.Id,
                SubscriptionPlan = plan,
                ActionType = "Renew",
                PeriodEndDate = now.AddDays(-2),
                CreatedAt = now.AddDays(-1)
            }
        };

        _mockContext.Setup(x => x.Enterprises).Returns(enterprises.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(new List<Employee>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.SubscriptionHistories).Returns(histories.AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetPlatformStatsQuery(), CancellationToken.None);

        result.RenewalRate.Should().Be(100);
    }

    [Fact]
    public async Task Handle_ShouldCountOnlyRenewActions_InsideRenewalWindow()
    {
        var now = DateTime.UtcNow;
        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Pro",
            PlanCode = "PRO",
            PriceMonthly = 1000000
        };
        var includedEnterpriseId = Guid.NewGuid();
        var excludedEnterpriseId = Guid.NewGuid();
        var enterprises = new List<Enterprise>
        {
            new()
            {
                Id = includedEnterpriseId,
                EnterpriseName = "Included Renew Co",
                Status = EnterpriseStatus.Active,
                SubscriptionPlan = plan,
                SubscriptionPlanId = plan.Id,
                SubscriptionEndDate = now.AddDays(15),
                IsDeleted = false
            },
            new()
            {
                Id = excludedEnterpriseId,
                EnterpriseName = "Excluded Renew Co",
                Status = EnterpriseStatus.Active,
                SubscriptionPlan = plan,
                SubscriptionPlanId = plan.Id,
                SubscriptionEndDate = now.AddDays(18),
                IsDeleted = false
            }
        };

        var histories = new List<SubscriptionHistory>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseId = includedEnterpriseId,
                Enterprise = enterprises[0],
                SubscriptionPlanId = plan.Id,
                SubscriptionPlan = plan,
                ActionType = "Renew",
                PeriodEndDate = now.AddDays(-5),
                CreatedAt = now.AddDays(-30).AddMinutes(1)
            },
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseId = excludedEnterpriseId,
                Enterprise = enterprises[1],
                SubscriptionPlanId = plan.Id,
                SubscriptionPlan = plan,
                ActionType = "Renew",
                PeriodEndDate = now.AddDays(-6),
                CreatedAt = now.AddDays(-30).AddMinutes(-1)
            }
        };

        _mockContext.Setup(x => x.Enterprises).Returns(enterprises.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(new List<Employee>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.SubscriptionHistories).Returns(histories.AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetPlatformStatsQuery(), CancellationToken.None);

        result.RenewalRate.Should().Be(50);
    }

    [Fact]
    public async Task Handle_ShouldUseZeroDayRiskReason_WhenSubscriptionEndsLaterToday()
    {
        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Pro",
            PlanCode = "PRO",
            PriceMonthly = 1000000
        };
        var enterpriseId = Guid.NewGuid();
        var enterprises = new List<Enterprise>
        {
            new()
            {
                Id = enterpriseId,
                EnterpriseName = "Today Expiry Co",
                Status = EnterpriseStatus.Active,
                SubscriptionPlan = plan,
                SubscriptionPlanId = plan.Id,
                SubscriptionEndDate = DateTime.UtcNow.Date.AddHours(23).AddMinutes(59),
                IsDeleted = false
            }
        };

        _mockContext.Setup(x => x.Enterprises).Returns(enterprises.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(new List<Employee>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.SubscriptionHistories).Returns(new List<SubscriptionHistory>().AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetPlatformStatsQuery(), CancellationToken.None);

        result.ChurnWatchlist.Should().ContainSingle(item => item.EnterpriseId == enterpriseId);
        result.ChurnWatchlist
            .Single(item => item.EnterpriseId == enterpriseId)
            .RiskReason.Should().Contain("0");
    }
}
