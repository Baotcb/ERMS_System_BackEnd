using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERMS.Application.Features.Admin.Queries.GetAdminDashboard;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Enterprise;
using ERMS.Domain.Entities.Enterprise;
using ERMS.Domain.Entities.Recruitment;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace ERMS.UnitTests.Features.Admin.Queries.GetAdminDashboard;

public class GetAdminDashboardHandlerTests
{
    private readonly Mock<IERMSDbContext> _mockContext;
    private readonly GetAdminDashboardHandler _handler;

    public GetAdminDashboardHandlerTests()
    {
        _mockContext = new Mock<IERMSDbContext>();
        _handler = new GetAdminDashboardHandler(_mockContext.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnAggregateCounts_AndRealAdminActivity()
    {
        var now = DateTime.UtcNow;
        var lockedId = Guid.NewGuid();
        var activeId = Guid.NewGuid();

        var enterprises = new List<Enterprise>
        {
            new() { Id = lockedId, EnterpriseName = "Locked Corp", EnterpriseCode = "LC001", Status = EnterpriseStatus.Locked, SubscriptionEndDate = now.AddDays(60), CreatedAt = now.AddDays(-1), IsDeleted = false },
            new() { Id = Guid.NewGuid(), EnterpriseName = "Suspended Corp", EnterpriseCode = "SC001", Status = EnterpriseStatus.Suspended, SubscriptionEndDate = now.AddDays(60), CreatedAt = now.AddDays(-2), IsDeleted = false },
            new() { Id = activeId, EnterpriseName = "Expiring Corp", EnterpriseCode = "EC001", Status = EnterpriseStatus.Active, SubscriptionEndDate = now.AddDays(10), CreatedAt = now.AddDays(-3), IsDeleted = false },
            new() { Id = Guid.NewGuid(), EnterpriseName = "Deleted Corp", EnterpriseCode = "DC001", Status = EnterpriseStatus.Locked, SubscriptionEndDate = now.AddDays(5), CreatedAt = now.AddDays(-4), IsDeleted = true }
        };

        var plan = new SubscriptionPlan { Id = Guid.NewGuid(), PlanName = "Pro" };
        var approvalHistories = new List<ApprovalHistory>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EntityType = "Enterprise",
                EntityId = lockedId,
                Action = "StatusChange",
                PreviousStatus = EnterpriseStatus.Active,
                NewStatus = EnterpriseStatus.Locked,
                PerformedById = Guid.NewGuid(),
                PerformedBy = new ERMS.Domain.Entities.Identity.User { FullName = "Admin A" },
                CreatedAt = now.AddHours(-1)
            }
        };
        var payments = new List<SubscriptionHistory>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseId = lockedId,
                Enterprise = enterprises[0],
                SubscriptionPlanId = plan.Id,
                SubscriptionPlan = plan,
                ActionType = "Renew",
                Amount = 1500000,
                CreatedAt = now.AddHours(-2)
            },
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterprises[2].Id,
                Enterprise = enterprises[2],
                SubscriptionPlanId = plan.Id,
                SubscriptionPlan = plan,
                ActionType = "Subscribe",
                Amount = 1200000,
                CreatedAt = now.AddHours(-5)
            }
        };

        _mockContext.Setup(x => x.Enterprises).Returns(enterprises.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.ApprovalHistories).Returns(approvalHistories.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.SubscriptionHistories).Returns(payments.AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetAdminDashboardQuery(), CancellationToken.None);

        result.TotalEnterprises.Should().Be(3);
        result.ActiveEnterprises.Should().Be(1);
        result.LockedEnterprises.Should().Be(1);
        result.ExpiringSoonEnterprises.Should().Be(1);
        result.AttentionItems.Should().Contain(item => item.EnterpriseId == lockedId);
        result.AttentionItems.Should().Contain(item => item.EnterpriseId == activeId);
        result.RecentActivities.Should().ContainSingle();
        result.RecentActivities[0].EnterpriseName.Should().Be("Locked Corp");
        result.RecentPayments.Should().HaveCount(2);
        result.RecentPayments[0].EnterpriseName.Should().Be("Locked Corp");
    }

    [Fact]
    public async Task Handle_ShouldExcludeExpiredActiveEnterprises_FromExpiringSoonCount()
    {
        var now = DateTime.UtcNow;

        var enterprises = new List<Enterprise>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseName = "Expired Active Corp",
                EnterpriseCode = "EA001",
                Status = EnterpriseStatus.Active,
                SubscriptionEndDate = now.AddDays(-1),
                CreatedAt = now.AddDays(-5),
                IsDeleted = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseName = "Expiring Soon Corp",
                EnterpriseCode = "ES001",
                Status = EnterpriseStatus.Active,
                SubscriptionEndDate = now.AddDays(7),
                CreatedAt = now.AddDays(-6),
                IsDeleted = false
            }
        };

        _mockContext.Setup(x => x.Enterprises).Returns(enterprises.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.ApprovalHistories).Returns(new List<ApprovalHistory>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.SubscriptionHistories).Returns(new List<SubscriptionHistory>().AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetAdminDashboardQuery(), CancellationToken.None);

        result.ExpiringSoonEnterprises.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldOrderRecentLockedEnterprises_ByLatestLockHistory()
    {
        var now = DateTime.UtcNow;

        var enterpriseCreatedNewest = new Enterprise
        {
            Id = Guid.NewGuid(),
            EnterpriseName = "Newest Created",
            EnterpriseCode = "NC001",
            Status = EnterpriseStatus.Locked,
            SubscriptionEndDate = now.AddDays(60),
            CreatedAt = now.AddDays(-1),
            IsDeleted = false
        };

        var enterpriseCreatedOldest = new Enterprise
        {
            Id = Guid.NewGuid(),
            EnterpriseName = "Oldest Created",
            EnterpriseCode = "OC001",
            Status = EnterpriseStatus.Locked,
            SubscriptionEndDate = now.AddDays(60),
            CreatedAt = now.AddDays(-10),
            IsDeleted = false
        };

        var enterpriseCreatedMiddle = new Enterprise
        {
            Id = Guid.NewGuid(),
            EnterpriseName = "Middle Created",
            EnterpriseCode = "MC001",
            Status = EnterpriseStatus.Locked,
            SubscriptionEndDate = now.AddDays(60),
            CreatedAt = now.AddDays(-5),
            IsDeleted = false
        };

        var approvalHistories = new List<ApprovalHistory>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EntityType = "Enterprise",
                EntityId = enterpriseCreatedNewest.Id,
                Action = "Lock",
                PreviousStatus = EnterpriseStatus.Active,
                NewStatus = EnterpriseStatus.Locked,
                PerformedById = Guid.NewGuid(),
                CreatedAt = now.AddHours(-8)
            },
            new()
            {
                Id = Guid.NewGuid(),
                EntityType = "Enterprise",
                EntityId = enterpriseCreatedOldest.Id,
                Action = "Lock",
                PreviousStatus = EnterpriseStatus.Active,
                NewStatus = EnterpriseStatus.Locked,
                PerformedById = Guid.NewGuid(),
                CreatedAt = now.AddHours(-1)
            },
            new()
            {
                Id = Guid.NewGuid(),
                EntityType = "Enterprise",
                EntityId = enterpriseCreatedMiddle.Id,
                Action = "Lock",
                PreviousStatus = EnterpriseStatus.Active,
                NewStatus = EnterpriseStatus.Locked,
                PerformedById = Guid.NewGuid(),
                CreatedAt = now.AddHours(-3)
            }
        };

        var enterprises = new List<Enterprise>
        {
            enterpriseCreatedNewest,
            enterpriseCreatedOldest,
            enterpriseCreatedMiddle
        };

        _mockContext.Setup(x => x.Enterprises).Returns(enterprises.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.SubscriptionHistories).Returns(new List<SubscriptionHistory>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.ApprovalHistories).Returns(approvalHistories.AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetAdminDashboardQuery(), CancellationToken.None);

        result.RecentActivities.Should().HaveCount(3);
        result.RecentActivities[0].EnterpriseName.Should().Be("Oldest Created");
        result.RecentActivities[1].EnterpriseName.Should().Be("Middle Created");
        result.RecentActivities[2].EnterpriseName.Should().Be("Newest Created");
    }

    [Fact]
    public async Task Handle_ShouldLimitAttentionItemsToSix_AndOrderByPriorityThenExpiry()
    {
        var now = DateTime.UtcNow;
        var enterprises = new List<Enterprise>
        {
            new() { Id = Guid.NewGuid(), EnterpriseName = "Locked Co", EnterpriseCode = "L001", Status = EnterpriseStatus.Locked, SubscriptionEndDate = now.AddDays(60), IsDeleted = false },
            new() { Id = Guid.NewGuid(), EnterpriseName = "Suspended Co", EnterpriseCode = "S001", Status = EnterpriseStatus.Suspended, SubscriptionEndDate = now.AddDays(50), IsDeleted = false },
            new() { Id = Guid.NewGuid(), EnterpriseName = "Inactive Co", EnterpriseCode = "I001", Status = EnterpriseStatus.Inactive, SubscriptionEndDate = now.AddDays(40), IsDeleted = false },
            new() { Id = Guid.NewGuid(), EnterpriseName = "Expired Co", EnterpriseCode = "E001", Status = EnterpriseStatus.Active, SubscriptionEndDate = now.AddDays(-2), IsDeleted = false },
            new() { Id = Guid.NewGuid(), EnterpriseName = "Expiring 5", EnterpriseCode = "X005", Status = EnterpriseStatus.Active, SubscriptionEndDate = now.AddDays(5), IsDeleted = false },
            new() { Id = Guid.NewGuid(), EnterpriseName = "Expiring 20", EnterpriseCode = "X020", Status = EnterpriseStatus.Active, SubscriptionEndDate = now.AddDays(20), IsDeleted = false },
            new() { Id = Guid.NewGuid(), EnterpriseName = "Expiring 25", EnterpriseCode = "X025", Status = EnterpriseStatus.Active, SubscriptionEndDate = now.AddDays(25), IsDeleted = false },
            new() { Id = Guid.NewGuid(), EnterpriseName = "Healthy Co", EnterpriseCode = "H001", Status = EnterpriseStatus.Active, SubscriptionEndDate = now.AddDays(90), IsDeleted = false }
        };

        _mockContext.Setup(x => x.Enterprises).Returns(enterprises.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.ApprovalHistories).Returns(new List<ApprovalHistory>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.SubscriptionHistories).Returns(new List<SubscriptionHistory>().AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetAdminDashboardQuery(), CancellationToken.None);

        result.AttentionItems.Should().HaveCount(6);
        result.AttentionItems.Select(item => item.EnterpriseName).Should().Equal(
            "Locked Co",
            "Suspended Co",
            "Inactive Co",
            "Expired Co",
            "Expiring 5",
            "Expiring 20");
        result.AttentionItems.Should().NotContain(item => item.EnterpriseName == "Healthy Co");
        result.AttentionItems.Should().NotContain(item => item.EnterpriseName == "Expiring 25");
    }

    [Fact]
    public async Task Handle_ShouldIncludeActiveEnterpriseExpiringExactlyOnThirtyDayThreshold()
    {
        var now = DateTime.UtcNow;
        var thresholdEnterpriseId = Guid.NewGuid();
        var enterprises = new List<Enterprise>
        {
            new()
            {
                Id = thresholdEnterpriseId,
                EnterpriseName = "Threshold Co",
                EnterpriseCode = "TH001",
                Status = EnterpriseStatus.Active,
                SubscriptionEndDate = now.AddDays(30),
                IsDeleted = false
            }
        };

        _mockContext.Setup(x => x.Enterprises).Returns(enterprises.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.ApprovalHistories).Returns(new List<ApprovalHistory>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.SubscriptionHistories).Returns(new List<SubscriptionHistory>().AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetAdminDashboardQuery(), CancellationToken.None);

        result.ExpiringSoonEnterprises.Should().Be(1);
        result.AttentionItems.Should().ContainSingle(item =>
            item.EnterpriseId == thresholdEnterpriseId &&
            item.AttentionReason == "Subscription sap het han");
    }

    [Fact]
    public async Task Handle_ShouldIgnoreNonEnterpriseHistory_AndReturnBlankChangedByName_WhenActorMissing()
    {
        var enterpriseId = Guid.NewGuid();
        var enterprises = new List<Enterprise>
        {
            new()
            {
                Id = enterpriseId,
                EnterpriseName = "Tech Corp",
                EnterpriseCode = "TC001",
                Status = EnterpriseStatus.Active,
                SubscriptionEndDate = DateTime.UtcNow.AddDays(20),
                IsDeleted = false
            }
        };

        var approvalHistories = new List<ApprovalHistory>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EntityType = "Enterprise",
                EntityId = enterpriseId,
                Action = "Suspend",
                PreviousStatus = EnterpriseStatus.Active,
                NewStatus = EnterpriseStatus.Suspended,
                PerformedBy = null,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                EntityType = "JobPosting",
                EntityId = Guid.NewGuid(),
                Action = "Archive",
                CreatedAt = DateTime.UtcNow.AddMinutes(-5)
            }
        };

        _mockContext.Setup(x => x.Enterprises).Returns(enterprises.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.ApprovalHistories).Returns(approvalHistories.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.SubscriptionHistories).Returns(new List<SubscriptionHistory>().AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetAdminDashboardQuery(), CancellationToken.None);

        result.RecentActivities.Should().ContainSingle();
        result.RecentActivities[0].EnterpriseName.Should().Be("Tech Corp");
        result.RecentActivities[0].ChangedByName.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnBlankEnterpriseMetadata_WhenActivityEnterpriseCannotBeResolved()
    {
        var approvalHistories = new List<ApprovalHistory>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EntityType = "Enterprise",
                EntityId = Guid.NewGuid(),
                Action = "Lock",
                PreviousStatus = EnterpriseStatus.Active,
                NewStatus = EnterpriseStatus.Locked,
                CreatedAt = DateTime.UtcNow
            }
        };

        _mockContext.Setup(x => x.Enterprises).Returns(new List<Enterprise>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.ApprovalHistories).Returns(approvalHistories.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.SubscriptionHistories).Returns(new List<SubscriptionHistory>().AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetAdminDashboardQuery(), CancellationToken.None);

        result.RecentActivities.Should().ContainSingle();
        result.RecentActivities[0].EnterpriseName.Should().BeEmpty();
        result.RecentActivities[0].EnterpriseCode.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnOnlyFiveRecentPayments_OrderedByNewestFirst()
    {
        var now = DateTime.UtcNow;
        var enterprise = new Enterprise
        {
            Id = Guid.NewGuid(),
            EnterpriseName = "Payments Co",
            EnterpriseCode = "PAY01",
            Status = EnterpriseStatus.Active,
            SubscriptionEndDate = now.AddDays(60),
            IsDeleted = false
        };
        var plan = new SubscriptionPlan { Id = Guid.NewGuid(), PlanName = "Pro" };
        var payments = Enumerable.Range(1, 6)
            .Select(index => new SubscriptionHistory
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterprise.Id,
                Enterprise = enterprise,
                SubscriptionPlanId = plan.Id,
                SubscriptionPlan = plan,
                ActionType = $"Action {index}",
                Amount = index * 1000,
                CreatedAt = now.AddHours(-index)
            })
            .ToList();

        _mockContext.Setup(x => x.Enterprises).Returns(new List<Enterprise> { enterprise }.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.ApprovalHistories).Returns(new List<ApprovalHistory>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.SubscriptionHistories).Returns(payments.AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetAdminDashboardQuery(), CancellationToken.None);

        result.RecentPayments.Should().HaveCount(5);
        result.RecentPayments.Select(item => item.Amount).Should().Equal(1000, 2000, 3000, 4000, 5000);
    }

    [Fact]
    public async Task Handle_ShouldLimitRecentActivitiesToSixNewestItems()
    {
        var now = DateTime.UtcNow;
        var enterprises = Enumerable.Range(1, 7)
            .Select(index => new Enterprise
            {
                Id = Guid.NewGuid(),
                EnterpriseName = $"Enterprise {index}",
                EnterpriseCode = $"EN{index:000}",
                Status = EnterpriseStatus.Active,
                SubscriptionEndDate = now.AddDays(60),
                IsDeleted = false
            })
            .ToList();

        var approvalHistories = enterprises
            .Select((enterprise, index) => new ApprovalHistory
            {
                Id = Guid.NewGuid(),
                EntityType = "Enterprise",
                EntityId = enterprise.Id,
                Action = $"Action {index + 1}",
                CreatedAt = now.AddMinutes(-index)
            })
            .ToList();

        _mockContext.Setup(x => x.Enterprises).Returns(enterprises.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.ApprovalHistories).Returns(approvalHistories.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.SubscriptionHistories).Returns(new List<SubscriptionHistory>().AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetAdminDashboardQuery(), CancellationToken.None);

        result.RecentActivities.Should().HaveCount(6);
        result.RecentActivities[0].Action.Should().Be("Action 1");
        result.RecentActivities[^1].Action.Should().Be("Action 6");
    }

    [Fact]
    public async Task Handle_ShouldReturnExpectedAttentionReason_ForSuspendedAndExpiredEnterprises()
    {
        var now = DateTime.UtcNow;
        var suspendedId = Guid.NewGuid();
        var expiredId = Guid.NewGuid();
        var enterprises = new List<Enterprise>
        {
            new()
            {
                Id = suspendedId,
                EnterpriseName = "Suspended Corp",
                EnterpriseCode = "SC001",
                Status = EnterpriseStatus.Suspended,
                SubscriptionEndDate = now.AddDays(60),
                IsDeleted = false
            },
            new()
            {
                Id = expiredId,
                EnterpriseName = "Expired Corp",
                EnterpriseCode = "EC001",
                Status = EnterpriseStatus.Active,
                SubscriptionEndDate = now.AddDays(-1),
                IsDeleted = false
            }
        };

        _mockContext.Setup(x => x.Enterprises).Returns(enterprises.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.ApprovalHistories).Returns(new List<ApprovalHistory>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.SubscriptionHistories).Returns(new List<SubscriptionHistory>().AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetAdminDashboardQuery(), CancellationToken.None);

        result.AttentionItems.Should().Contain(item => item.EnterpriseId == suspendedId && item.AttentionReason == "Doanh nghiep dang tam dung");
        result.AttentionItems.Should().Contain(item => item.EnterpriseId == expiredId && item.AttentionReason == "Subscription da het han");
    }
}
