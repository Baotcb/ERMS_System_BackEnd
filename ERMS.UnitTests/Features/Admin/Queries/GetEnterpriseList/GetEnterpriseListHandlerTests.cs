using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERMS.Application.Features.Admin;
using ERMS.Application.Features.Admin.Queries.GetEnterpriseList;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Enterprise;
using ERMS.Domain.Entities.Enterprise;
using ERMS.Domain.Entities.Organization;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace ERMS.UnitTests.Features.Admin.Queries.GetEnterpriseList;

public class GetEnterpriseListHandlerTests
{
    private readonly Mock<IERMSDbContext> _mockContext;
    private readonly GetEnterpriseListHandler _handler;

    public GetEnterpriseListHandlerTests()
    {
        _mockContext = new Mock<IERMSDbContext>();
        _handler = new GetEnterpriseListHandler(_mockContext.Object);
    }

    [Fact]
    public async Task Handle_ShouldFilterBySearchAndStatus_AndMapEmployeeCount()
    {
        var proPlan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Pro"
        };

        var matchingEnterpriseId = Guid.NewGuid();
        var enterprises = new List<Enterprise>
        {
            new()
            {
                Id = matchingEnterpriseId,
                EnterpriseName = "Tech Corp",
                EnterpriseCode = "TC001",
                Email = "contact@techcorp.vn",
                Phone = "0123456789",
                Status = EnterpriseStatus.Active,
                SubscriptionPlanId = proPlan.Id,
                SubscriptionPlan = proPlan,
                SubscriptionEndDate = new DateTime(2026, 6, 1),
                CreatedAt = new DateTime(2026, 1, 5),
                IsDeleted = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseName = "Retail Hub",
                EnterpriseCode = "RH002",
                Status = EnterpriseStatus.Active,
                SubscriptionPlanId = proPlan.Id,
                SubscriptionPlan = proPlan,
                SubscriptionEndDate = new DateTime(2026, 7, 1),
                CreatedAt = new DateTime(2026, 1, 10),
                IsDeleted = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseName = "Tech Locked",
                EnterpriseCode = "TL003",
                Status = EnterpriseStatus.Locked,
                SubscriptionPlanId = proPlan.Id,
                SubscriptionPlan = proPlan,
                SubscriptionEndDate = new DateTime(2026, 8, 1),
                CreatedAt = new DateTime(2026, 1, 15),
                IsDeleted = false
            }
        };

        var employees = new List<Employee>
        {
            new() { Id = Guid.NewGuid(), EnterpriseId = matchingEnterpriseId, EmployeeCode = "EMP001", UserId = Guid.NewGuid() },
            new() { Id = Guid.NewGuid(), EnterpriseId = matchingEnterpriseId, EmployeeCode = "EMP002", UserId = Guid.NewGuid() },
            new() { Id = Guid.NewGuid(), EnterpriseId = Guid.NewGuid(), EmployeeCode = "EMP003", UserId = Guid.NewGuid() }
        };

        _mockContext.Setup(x => x.Enterprises).Returns(enterprises.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(employees.AsQueryable().BuildMockDbSet().Object);

        var query = new GetEnterpriseListQuery
        {
            Search = "tech",
            Status = EnterpriseStatus.Active,
            PageNumber = 1,
            PageSize = 10
        };

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);

        var item = result.Items.Single();
        item.EnterpriseId.Should().Be(matchingEnterpriseId);
        item.EnterpriseName.Should().Be("Tech Corp");
        item.EnterpriseCode.Should().Be("TC001");
        item.Status.Should().Be(EnterpriseStatus.Active);
        item.ContactEmail.Should().Be("contact@techcorp.vn");
        item.ContactPhone.Should().Be("0123456789");
        item.CurrentPlanName.Should().Be("Pro");
        item.SubscriptionEndDate.Should().Be(new DateTime(2026, 6, 1));
        item.CreatedAt.Should().Be(new DateTime(2026, 1, 5));
        item.EmployeeCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ShouldExcludeSoftDeletedEnterprises()
    {
        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Basic"
        };

        var enterprises = new List<Enterprise>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseName = "Visible",
                EnterpriseCode = "VI001",
                Status = EnterpriseStatus.Active,
                SubscriptionPlanId = plan.Id,
                SubscriptionPlan = plan,
                SubscriptionEndDate = new DateTime(2026, 6, 1),
                CreatedAt = new DateTime(2026, 1, 1),
                IsDeleted = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseName = "Deleted",
                EnterpriseCode = "DE002",
                Status = EnterpriseStatus.Active,
                SubscriptionPlanId = plan.Id,
                SubscriptionPlan = plan,
                SubscriptionEndDate = new DateTime(2026, 6, 2),
                CreatedAt = new DateTime(2026, 1, 2),
                IsDeleted = true
            }
        };

        _mockContext.Setup(x => x.Enterprises).Returns(enterprises.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(new List<Employee>().AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetEnterpriseListQuery { PageNumber = 1, PageSize = 10 }, CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items[0].EnterpriseName.Should().Be("Visible");
    }

    [Fact]
    public async Task Handle_ShouldPaginateResults()
    {
        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Growth"
        };

        var enterprises = Enumerable.Range(1, 3)
            .Select(index => new Enterprise
            {
                Id = Guid.NewGuid(),
                EnterpriseName = $"Enterprise {index}",
                EnterpriseCode = $"EN00{index}",
                Status = EnterpriseStatus.Active,
                SubscriptionPlanId = plan.Id,
                SubscriptionPlan = plan,
                SubscriptionEndDate = new DateTime(2026, 6, index),
                CreatedAt = new DateTime(2026, 1, index),
                IsDeleted = false
            })
            .ToList();

        _mockContext.Setup(x => x.Enterprises).Returns(enterprises.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(new List<Employee>().AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetEnterpriseListQuery { PageNumber = 2, PageSize = 1 }, CancellationToken.None);

        result.TotalCount.Should().Be(3);
        result.Items.Should().ContainSingle();
        result.Items[0].EnterpriseName.Should().Be("Enterprise 2");
    }

    [Fact]
    public async Task Handle_ShouldFilterByPlanTier_UsingNormalizedBusinessTiers()
    {
        var freePlan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Basic",
            PlanCode = "BASIC"
        };

        var proPlan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Enterprise",
            PlanCode = "ENT"
        };

        var enterprises = new List<Enterprise>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseName = "Free Co",
                EnterpriseCode = "FR001",
                Status = EnterpriseStatus.Active,
                SubscriptionPlanId = freePlan.Id,
                SubscriptionPlan = freePlan,
                SubscriptionEndDate = new DateTime(2026, 6, 1),
                CreatedAt = new DateTime(2026, 1, 1),
                IsDeleted = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseName = "Pro Co",
                EnterpriseCode = "PR001",
                Status = EnterpriseStatus.Active,
                SubscriptionPlanId = proPlan.Id,
                SubscriptionPlan = proPlan,
                SubscriptionEndDate = new DateTime(2026, 6, 2),
                CreatedAt = new DateTime(2026, 1, 2),
                IsDeleted = false
            }
        };

        _mockContext.Setup(x => x.Enterprises).Returns(enterprises.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(new List<Employee>().AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(
            new GetEnterpriseListQuery
            {
                PlanTier = "Pro",
                PageNumber = 1,
                PageSize = 10
            },
            CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items[0].EnterpriseName.Should().Be("Pro Co");
        result.Items[0].CurrentPlanCode.Should().Be("ENT");
    }

    [Fact]
    public async Task Handle_ShouldKeepLegacyRawPlanCodeFiltering_WhenPlanTierIsNotProvided()
    {
        var freePlan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Free",
            PlanCode = "FREE"
        };

        var proPlan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Pro",
            PlanCode = "PRO"
        };

        var enterprises = new List<Enterprise>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseName = "Legacy Free",
                EnterpriseCode = "LF001",
                Status = EnterpriseStatus.Active,
                SubscriptionPlanId = freePlan.Id,
                SubscriptionPlan = freePlan,
                SubscriptionEndDate = new DateTime(2026, 6, 1),
                CreatedAt = new DateTime(2026, 1, 1),
                IsDeleted = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseName = "Legacy Pro",
                EnterpriseCode = "LP001",
                Status = EnterpriseStatus.Active,
                SubscriptionPlanId = proPlan.Id,
                SubscriptionPlan = proPlan,
                SubscriptionEndDate = new DateTime(2026, 6, 2),
                CreatedAt = new DateTime(2026, 1, 2),
                IsDeleted = false
            }
        };

        _mockContext.Setup(x => x.Enterprises).Returns(enterprises.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(new List<Employee>().AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(
            new GetEnterpriseListQuery
            {
                PlanCode = "FREE",
                PageNumber = 1,
                PageSize = 10
            },
            CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items[0].EnterpriseName.Should().Be("Legacy Free");
        result.Items[0].CurrentPlanCode.Should().Be("FREE");
    }

    [Fact]
    public async Task Handle_ShouldMatchSharedTierClassification_ForPlanTierFiltering()
    {
        var growthPlan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Starter",
            PlanCode = "GROWTH"
        };

        var enterpriseNamedPlan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Enterprise Plus",
            PlanCode = "CUSTOM"
        };

        var freePlan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Starter",
            PlanCode = "FREE"
        };

        var enterprises = new List<Enterprise>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseName = "Growth Code",
                EnterpriseCode = "GC001",
                Status = EnterpriseStatus.Active,
                SubscriptionPlanId = growthPlan.Id,
                SubscriptionPlan = growthPlan,
                SubscriptionEndDate = new DateTime(2026, 6, 1),
                CreatedAt = new DateTime(2026, 1, 1),
                IsDeleted = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseName = "Enterprise Name",
                EnterpriseCode = "EN001",
                Status = EnterpriseStatus.Active,
                SubscriptionPlanId = enterpriseNamedPlan.Id,
                SubscriptionPlan = enterpriseNamedPlan,
                SubscriptionEndDate = new DateTime(2026, 6, 2),
                CreatedAt = new DateTime(2026, 1, 2),
                IsDeleted = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseName = "Free Tier",
                EnterpriseCode = "FT001",
                Status = EnterpriseStatus.Active,
                SubscriptionPlanId = freePlan.Id,
                SubscriptionPlan = freePlan,
                SubscriptionEndDate = new DateTime(2026, 6, 3),
                CreatedAt = new DateTime(2026, 1, 3),
                IsDeleted = false
            }
        };

        _mockContext.Setup(x => x.Enterprises).Returns(enterprises.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(new List<Employee>().AsQueryable().BuildMockDbSet().Object);

        var expectedEnterpriseNames = enterprises
            .Where(enterprise => EnterprisePlanTier.GetTierName(enterprise.SubscriptionPlan.PlanName, enterprise.SubscriptionPlan.PlanCode) == EnterprisePlanTier.Pro)
            .Select(enterprise => enterprise.EnterpriseName)
            .OrderBy(name => name)
            .ToList();

        var result = await _handler.Handle(
            new GetEnterpriseListQuery
            {
                PlanTier = EnterprisePlanTier.Pro,
                PageNumber = 1,
                PageSize = 10
            },
            CancellationToken.None);

        result.Items
            .Select(item => item.EnterpriseName)
            .OrderBy(name => name)
            .Should()
            .Equal(expectedEnterpriseNames);
    }

    [Fact]
    public async Task Handle_ShouldIgnoreInvalidPlanTierInput_InsteadOfCoercingToFree()
    {
        var freePlan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Basic",
            PlanCode = "FREE"
        };

        var proPlan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Pro",
            PlanCode = "PRO"
        };

        var enterprises = new List<Enterprise>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseName = "Free Enterprise",
                EnterpriseCode = "FE001",
                Status = EnterpriseStatus.Active,
                SubscriptionPlanId = freePlan.Id,
                SubscriptionPlan = freePlan,
                SubscriptionEndDate = new DateTime(2026, 6, 1),
                CreatedAt = new DateTime(2026, 1, 1),
                IsDeleted = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseName = "Pro Enterprise",
                EnterpriseCode = "PE001",
                Status = EnterpriseStatus.Active,
                SubscriptionPlanId = proPlan.Id,
                SubscriptionPlan = proPlan,
                SubscriptionEndDate = new DateTime(2026, 6, 2),
                CreatedAt = new DateTime(2026, 1, 2),
                IsDeleted = false
            }
        };

        _mockContext.Setup(x => x.Enterprises).Returns(enterprises.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(new List<Employee>().AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(
            new GetEnterpriseListQuery
            {
                PlanTier = "unknown-tier",
                PageNumber = 1,
                PageSize = 10
            },
            CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ShouldFilterByExpiringWithinDays_AndExcludeExpiredSubscriptions()
    {
        var now = DateTime.UtcNow;
        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Pro",
            PlanCode = "PRO"
        };

        var enterprises = new List<Enterprise>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseName = "Expiring Soon",
                EnterpriseCode = "ES001",
                Status = EnterpriseStatus.Active,
                SubscriptionPlanId = plan.Id,
                SubscriptionPlan = plan,
                SubscriptionEndDate = now.AddDays(5),
                CreatedAt = new DateTime(2026, 1, 3),
                IsDeleted = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseName = "Far Future",
                EnterpriseCode = "FF001",
                Status = EnterpriseStatus.Active,
                SubscriptionPlanId = plan.Id,
                SubscriptionPlan = plan,
                SubscriptionEndDate = now.AddDays(20),
                CreatedAt = new DateTime(2026, 1, 2),
                IsDeleted = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseName = "Expired",
                EnterpriseCode = "EX001",
                Status = EnterpriseStatus.Active,
                SubscriptionPlanId = plan.Id,
                SubscriptionPlan = plan,
                SubscriptionEndDate = now.AddDays(-1),
                CreatedAt = new DateTime(2026, 1, 1),
                IsDeleted = false
            }
        };

        _mockContext.Setup(x => x.Enterprises).Returns(enterprises.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(new List<Employee>().AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(
            new GetEnterpriseListQuery
            {
                ExpiringWithinDays = 10,
                PageNumber = 1,
                PageSize = 10
            },
            CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items[0].EnterpriseName.Should().Be("Expiring Soon");
    }

    [Fact]
    public async Task Handle_ShouldSearchCaseInsensitiveAcrossEmailAndPhone()
    {
        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Free",
            PlanCode = "FREE"
        };
        var enterprises = new List<Enterprise>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseName = "Alpha",
                EnterpriseCode = "A001",
                Email = "support@alpha.vn",
                Phone = "0909123456",
                Status = EnterpriseStatus.Active,
                SubscriptionPlanId = plan.Id,
                SubscriptionPlan = plan,
                SubscriptionEndDate = new DateTime(2026, 6, 1),
                CreatedAt = new DateTime(2026, 1, 1),
                IsDeleted = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseName = "Beta",
                EnterpriseCode = "B001",
                Email = "hello@beta.vn",
                Phone = "0999888777",
                Status = EnterpriseStatus.Active,
                SubscriptionPlanId = plan.Id,
                SubscriptionPlan = plan,
                SubscriptionEndDate = new DateTime(2026, 6, 2),
                CreatedAt = new DateTime(2026, 1, 2),
                IsDeleted = false
            }
        };

        _mockContext.Setup(x => x.Enterprises).Returns(enterprises.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(new List<Employee>().AsQueryable().BuildMockDbSet().Object);

        var emailResult = await _handler.Handle(
            new GetEnterpriseListQuery { Search = "SUPPORT@ALPHA", PageNumber = 1, PageSize = 10 },
            CancellationToken.None);
        var phoneResult = await _handler.Handle(
            new GetEnterpriseListQuery { Search = "888777", PageNumber = 1, PageSize = 10 },
            CancellationToken.None);

        emailResult.Items.Should().ContainSingle();
        emailResult.Items[0].EnterpriseName.Should().Be("Alpha");
        phoneResult.Items.Should().ContainSingle();
        phoneResult.Items[0].EnterpriseName.Should().Be("Beta");
    }

    [Fact]
    public async Task Handle_ShouldDefaultInvalidPaginationValues()
    {
        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Pro",
            PlanCode = "PRO"
        };
        var enterprises = Enumerable.Range(1, 2)
            .Select(index => new Enterprise
            {
                Id = Guid.NewGuid(),
                EnterpriseName = $"Enterprise {index}",
                EnterpriseCode = $"E{index:000}",
                Status = EnterpriseStatus.Active,
                SubscriptionPlanId = plan.Id,
                SubscriptionPlan = plan,
                SubscriptionEndDate = new DateTime(2026, 6, index),
                CreatedAt = new DateTime(2026, 1, index),
                IsDeleted = false
            })
            .ToList();

        _mockContext.Setup(x => x.Enterprises).Returns(enterprises.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(new List<Employee>().AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(
            new GetEnterpriseListQuery
            {
                PageNumber = 0,
                PageSize = 0
            },
            CancellationToken.None);

        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(1);
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ShouldIgnoreDeletedEmployees_WhenMappingEmployeeCount()
    {
        var enterpriseId = Guid.NewGuid();
        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Pro",
            PlanCode = "PRO"
        };
        var enterprises = new List<Enterprise>
        {
            new()
            {
                Id = enterpriseId,
                EnterpriseName = "Headcount Co",
                EnterpriseCode = "HC001",
                Status = EnterpriseStatus.Active,
                SubscriptionPlanId = plan.Id,
                SubscriptionPlan = plan,
                SubscriptionEndDate = new DateTime(2026, 6, 1),
                CreatedAt = new DateTime(2026, 1, 1),
                IsDeleted = false
            }
        };
        var employees = new List<Employee>
        {
            new() { Id = Guid.NewGuid(), EnterpriseId = enterpriseId, EmployeeCode = "E1", UserId = Guid.NewGuid(), IsDeleted = false },
            new() { Id = Guid.NewGuid(), EnterpriseId = enterpriseId, EmployeeCode = "E2", UserId = Guid.NewGuid(), IsDeleted = true }
        };

        _mockContext.Setup(x => x.Enterprises).Returns(enterprises.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(employees.AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetEnterpriseListQuery { PageNumber = 1, PageSize = 10 }, CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items[0].EmployeeCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldOrderByCreatedAtDescendingBeforeApplyingPagination()
    {
        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Pro",
            PlanCode = "PRO"
        };
        var enterprises = new List<Enterprise>
        {
            new() { Id = Guid.NewGuid(), EnterpriseName = "Oldest", EnterpriseCode = "O001", Status = EnterpriseStatus.Active, SubscriptionPlanId = plan.Id, SubscriptionPlan = plan, SubscriptionEndDate = new DateTime(2026, 6, 1), CreatedAt = new DateTime(2026, 1, 1), IsDeleted = false },
            new() { Id = Guid.NewGuid(), EnterpriseName = "Newest", EnterpriseCode = "N001", Status = EnterpriseStatus.Active, SubscriptionPlanId = plan.Id, SubscriptionPlan = plan, SubscriptionEndDate = new DateTime(2026, 6, 2), CreatedAt = new DateTime(2026, 1, 3), IsDeleted = false },
            new() { Id = Guid.NewGuid(), EnterpriseName = "Middle", EnterpriseCode = "M001", Status = EnterpriseStatus.Active, SubscriptionPlanId = plan.Id, SubscriptionPlan = plan, SubscriptionEndDate = new DateTime(2026, 6, 3), CreatedAt = new DateTime(2026, 1, 2), IsDeleted = false }
        };

        _mockContext.Setup(x => x.Enterprises).Returns(enterprises.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(new List<Employee>().AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(
            new GetEnterpriseListQuery
            {
                PageNumber = 1,
                PageSize = 2
            },
            CancellationToken.None);

        result.Items.Select(item => item.EnterpriseName).Should().Equal("Newest", "Middle");
    }

    [Fact]
    public async Task Handle_ShouldIgnoreNonPositiveExpiringWithinDaysValues()
    {
        var now = DateTime.UtcNow;
        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Pro",
            PlanCode = "PRO"
        };
        var enterprises = new List<Enterprise>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseName = "Expired Enterprise",
                EnterpriseCode = "EX001",
                Status = EnterpriseStatus.Active,
                SubscriptionPlanId = plan.Id,
                SubscriptionPlan = plan,
                SubscriptionEndDate = now.AddDays(-3),
                CreatedAt = new DateTime(2026, 1, 1),
                IsDeleted = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseName = "Future Enterprise",
                EnterpriseCode = "FU001",
                Status = EnterpriseStatus.Active,
                SubscriptionPlanId = plan.Id,
                SubscriptionPlan = plan,
                SubscriptionEndDate = now.AddDays(40),
                CreatedAt = new DateTime(2026, 1, 2),
                IsDeleted = false
            }
        };

        _mockContext.Setup(x => x.Enterprises).Returns(enterprises.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(new List<Employee>().AsQueryable().BuildMockDbSet().Object);

        var zeroResult = await _handler.Handle(new GetEnterpriseListQuery { ExpiringWithinDays = 0, PageNumber = 1, PageSize = 10 }, CancellationToken.None);
        var negativeResult = await _handler.Handle(new GetEnterpriseListQuery { ExpiringWithinDays = -5, PageNumber = 1, PageSize = 10 }, CancellationToken.None);

        zeroResult.TotalCount.Should().Be(2);
        negativeResult.TotalCount.Should().Be(2);
    }
}
