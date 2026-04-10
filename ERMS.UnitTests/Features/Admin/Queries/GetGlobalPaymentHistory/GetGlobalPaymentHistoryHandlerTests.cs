using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERMS.Application.Features.Admin.Queries.GetGlobalPaymentHistory;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Enterprise;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace ERMS.UnitTests.Features.Admin.Queries.GetGlobalPaymentHistory;

public class GetGlobalPaymentHistoryHandlerTests
{
    private readonly Mock<IERMSDbContext> _mockContext;
    private readonly GetGlobalPaymentHistoryHandler _handler;

    public GetGlobalPaymentHistoryHandlerTests()
    {
        _mockContext = new Mock<IERMSDbContext>();
        _handler = new GetGlobalPaymentHistoryHandler(_mockContext.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnCrossEnterprisePaymentHistory_OrderedByNewestFirst()
    {
        var proPlan = new SubscriptionPlan { Id = Guid.NewGuid(), PlanName = "Pro" };
        var basicPlan = new SubscriptionPlan { Id = Guid.NewGuid(), PlanName = "Basic" };

        var histories = new List<SubscriptionHistory>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseId = Guid.NewGuid(),
                Enterprise = new Enterprise { EnterpriseName = "Beta Corp", EnterpriseCode = "BE001" },
                SubscriptionPlanId = proPlan.Id,
                SubscriptionPlan = proPlan,
                PreviousPlanId = basicPlan.Id,
                PreviousPlan = basicPlan,
                ActionType = "Upgrade",
                Amount = 2000000,
                Currency = "VND",
                PaymentMethod = "BankTransfer",
                PeriodStartDate = new DateTime(2026, 2, 1),
                PeriodEndDate = new DateTime(2027, 1, 31),
                Note = "Yearly upgrade",
                CreatedAt = new DateTime(2026, 2, 10)
            },
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseId = Guid.NewGuid(),
                Enterprise = new Enterprise { EnterpriseName = "Alpha Corp", EnterpriseCode = "AL001" },
                SubscriptionPlanId = basicPlan.Id,
                SubscriptionPlan = basicPlan,
                ActionType = "Subscribe",
                Amount = 1000000,
                Currency = "VND",
                PaymentMethod = "Cash",
                PeriodStartDate = new DateTime(2026, 1, 1),
                PeriodEndDate = new DateTime(2026, 12, 31),
                Note = "Initial subscription",
                CreatedAt = new DateTime(2026, 1, 5)
            }
        };

        _mockContext.Setup(x => x.SubscriptionHistories).Returns(histories.AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetGlobalPaymentHistoryQuery { PageNumber = 1, PageSize = 10 }, CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items[0].EnterpriseName.Should().Be("Beta Corp");
        result.Items[0].PlanName.Should().Be("Pro");
        result.Items[0].PreviousPlanName.Should().Be("Basic");
        result.Items[1].EnterpriseName.Should().Be("Alpha Corp");
    }

    [Fact]
    public async Task Handle_ShouldFilterByEnterpriseSearch()
    {
        var plan = new SubscriptionPlan { Id = Guid.NewGuid(), PlanName = "Pro" };

        var histories = new List<SubscriptionHistory>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseId = Guid.NewGuid(),
                Enterprise = new Enterprise { EnterpriseName = "Tech Corp", EnterpriseCode = "TC001" },
                SubscriptionPlanId = plan.Id,
                SubscriptionPlan = plan,
                ActionType = "Subscribe",
                Amount = 1500000,
                CreatedAt = new DateTime(2026, 1, 2)
            },
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseId = Guid.NewGuid(),
                Enterprise = new Enterprise { EnterpriseName = "Retail Hub", EnterpriseCode = "RH002" },
                SubscriptionPlanId = plan.Id,
                SubscriptionPlan = plan,
                ActionType = "Subscribe",
                Amount = 1600000,
                CreatedAt = new DateTime(2026, 1, 3)
            }
        };

        _mockContext.Setup(x => x.SubscriptionHistories).Returns(histories.AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetGlobalPaymentHistoryQuery
        {
            EnterpriseSearch = "tech",
            PageNumber = 1,
            PageSize = 10
        }, CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items[0].EnterpriseCode.Should().Be("TC001");
    }

    [Fact]
    public async Task Handle_ShouldFilterByActionTypeDateRangeAndPaymentMethod()
    {
        var plan = new SubscriptionPlan { Id = Guid.NewGuid(), PlanName = "Pro", PlanCode = "PRO" };

        var histories = new List<SubscriptionHistory>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseId = Guid.NewGuid(),
                Enterprise = new Enterprise { EnterpriseName = "Tech Corp", EnterpriseCode = "TC001" },
                SubscriptionPlanId = plan.Id,
                SubscriptionPlan = plan,
                ActionType = "Renew",
                Amount = 1800000,
                Currency = "VND",
                PaymentMethod = "BankTransfer",
                PaymentReference = "PAY-001",
                PeriodStartDate = new DateTime(2026, 2, 1),
                PeriodEndDate = new DateTime(2026, 3, 1),
                CreatedAt = new DateTime(2026, 2, 15)
            },
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseId = Guid.NewGuid(),
                Enterprise = new Enterprise { EnterpriseName = "Retail Hub", EnterpriseCode = "RH002" },
                SubscriptionPlanId = plan.Id,
                SubscriptionPlan = plan,
                ActionType = "Upgrade",
                Amount = 2200000,
                Currency = "VND",
                PaymentMethod = "Card",
                PaymentReference = "PAY-002",
                PeriodStartDate = new DateTime(2026, 1, 1),
                PeriodEndDate = new DateTime(2026, 2, 1),
                CreatedAt = new DateTime(2026, 1, 10)
            }
        };

        _mockContext.Setup(x => x.SubscriptionHistories).Returns(histories.AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(
            new GetGlobalPaymentHistoryQuery
            {
                ActionType = "Renew",
                PaymentMethod = "BankTransfer",
                DateFrom = new DateTime(2026, 2, 1),
                DateTo = new DateTime(2026, 2, 28),
                PageNumber = 1,
                PageSize = 10
            },
            CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items[0].PaymentReference.Should().Be("PAY-001");
        result.Items[0].CreatedAt.Should().Be(new DateTime(2026, 2, 15));
    }

    [Fact]
    public async Task Handle_ShouldPaginateResults()
    {
        var plan = new SubscriptionPlan { Id = Guid.NewGuid(), PlanName = "Growth" };

        var histories = Enumerable.Range(1, 3)
            .Select(index => new SubscriptionHistory
            {
                Id = Guid.NewGuid(),
                EnterpriseId = Guid.NewGuid(),
                Enterprise = new Enterprise { EnterpriseName = $"Enterprise {index}", EnterpriseCode = $"EN00{index}" },
                SubscriptionPlanId = plan.Id,
                SubscriptionPlan = plan,
                ActionType = "Renew",
                Amount = 1000000 + index,
                CreatedAt = new DateTime(2026, 1, index)
            })
            .ToList();

        _mockContext.Setup(x => x.SubscriptionHistories).Returns(histories.AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetGlobalPaymentHistoryQuery { PageNumber = 2, PageSize = 1 }, CancellationToken.None);

        result.TotalCount.Should().Be(3);
        result.Items.Should().ContainSingle();
        result.Items[0].EnterpriseName.Should().Be("Enterprise 2");
    }

    [Fact]
    public async Task Handle_ShouldTreatDateToAsInclusiveForWholeDay()
    {
        var plan = new SubscriptionPlan { Id = Guid.NewGuid(), PlanName = "Pro", PlanCode = "PRO" };
        var histories = new List<SubscriptionHistory>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseId = Guid.NewGuid(),
                Enterprise = new Enterprise { EnterpriseName = "Included Corp", EnterpriseCode = "IC001" },
                SubscriptionPlanId = plan.Id,
                SubscriptionPlan = plan,
                ActionType = "Renew",
                Amount = 1000000,
                CreatedAt = new DateTime(2026, 2, 28, 23, 59, 59)
            },
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseId = Guid.NewGuid(),
                Enterprise = new Enterprise { EnterpriseName = "Excluded Corp", EnterpriseCode = "EC001" },
                SubscriptionPlanId = plan.Id,
                SubscriptionPlan = plan,
                ActionType = "Renew",
                Amount = 1200000,
                CreatedAt = new DateTime(2026, 3, 1, 0, 0, 0)
            }
        };

        _mockContext.Setup(x => x.SubscriptionHistories).Returns(histories.AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(
            new GetGlobalPaymentHistoryQuery
            {
                DateTo = new DateTime(2026, 2, 28),
                PageNumber = 1,
                PageSize = 10
            },
            CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items[0].EnterpriseName.Should().Be("Included Corp");
    }

    [Fact]
    public async Task Handle_ShouldReturnZeroTotalPages_WhenThereAreNoMatchingResults()
    {
        _mockContext.Setup(x => x.SubscriptionHistories).Returns(new List<SubscriptionHistory>().AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(
            new GetGlobalPaymentHistoryQuery
            {
                EnterpriseSearch = "missing",
                PageNumber = 1,
                PageSize = 10
            },
            CancellationToken.None);

        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldTreatDateFromAsInclusive()
    {
        var plan = new SubscriptionPlan { Id = Guid.NewGuid(), PlanName = "Pro", PlanCode = "PRO" };
        var histories = new List<SubscriptionHistory>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseId = Guid.NewGuid(),
                Enterprise = new Enterprise { EnterpriseName = "Included Corp", EnterpriseCode = "IC001" },
                SubscriptionPlanId = plan.Id,
                SubscriptionPlan = plan,
                ActionType = "Renew",
                Amount = 1000000,
                CreatedAt = new DateTime(2026, 2, 1, 0, 0, 0)
            },
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseId = Guid.NewGuid(),
                Enterprise = new Enterprise { EnterpriseName = "Excluded Corp", EnterpriseCode = "EC001" },
                SubscriptionPlanId = plan.Id,
                SubscriptionPlan = plan,
                ActionType = "Renew",
                Amount = 1100000,
                CreatedAt = new DateTime(2026, 1, 31, 23, 59, 59)
            }
        };

        _mockContext.Setup(x => x.SubscriptionHistories).Returns(histories.AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(
            new GetGlobalPaymentHistoryQuery
            {
                DateFrom = new DateTime(2026, 2, 1),
                PageNumber = 1,
                PageSize = 10
            },
            CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items[0].EnterpriseName.Should().Be("Included Corp");
    }

    [Fact]
    public async Task Handle_ShouldDefaultInvalidPaginationValues()
    {
        var plan = new SubscriptionPlan { Id = Guid.NewGuid(), PlanName = "Pro" };
        var histories = Enumerable.Range(1, 2)
            .Select(index => new SubscriptionHistory
            {
                Id = Guid.NewGuid(),
                EnterpriseId = Guid.NewGuid(),
                Enterprise = new Enterprise { EnterpriseName = $"Enterprise {index}", EnterpriseCode = $"EN00{index}" },
                SubscriptionPlanId = plan.Id,
                SubscriptionPlan = plan,
                ActionType = "Renew",
                Amount = 1000000 + index,
                CreatedAt = new DateTime(2026, 1, index)
            })
            .ToList();

        _mockContext.Setup(x => x.SubscriptionHistories).Returns(histories.AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(
            new GetGlobalPaymentHistoryQuery
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
    public async Task Handle_ShouldCapRequestedPageSizeToOneHundred()
    {
        var plan = new SubscriptionPlan { Id = Guid.NewGuid(), PlanName = "Pro" };
        var histories = Enumerable.Range(1, 150)
            .Select(index => new SubscriptionHistory
            {
                Id = Guid.NewGuid(),
                EnterpriseId = Guid.NewGuid(),
                Enterprise = new Enterprise { EnterpriseName = $"Enterprise {index}", EnterpriseCode = $"EN{index:000}" },
                SubscriptionPlanId = plan.Id,
                SubscriptionPlan = plan,
                ActionType = "Renew",
                Amount = 1000000 + index,
                CreatedAt = new DateTime(2026, 1, 1).AddMinutes(index)
            })
            .ToList();

        _mockContext.Setup(x => x.SubscriptionHistories).Returns(histories.AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(
            new GetGlobalPaymentHistoryQuery
            {
                PageNumber = 1,
                PageSize = 1000
            },
            CancellationToken.None);

        result.PageSize.Should().Be(100);
        result.Items.Should().HaveCount(100);
        result.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ShouldSearchByEnterpriseCode_CaseInsensitive()
    {
        var plan = new SubscriptionPlan { Id = Guid.NewGuid(), PlanName = "Pro" };
        var histories = new List<SubscriptionHistory>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseId = Guid.NewGuid(),
                Enterprise = new Enterprise { EnterpriseName = "Tech Corp", EnterpriseCode = "TC001" },
                SubscriptionPlanId = plan.Id,
                SubscriptionPlan = plan,
                ActionType = "Subscribe",
                Amount = 1000000,
                CreatedAt = new DateTime(2026, 1, 1)
            },
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseId = Guid.NewGuid(),
                Enterprise = new Enterprise { EnterpriseName = "Retail Hub", EnterpriseCode = "RH002" },
                SubscriptionPlanId = plan.Id,
                SubscriptionPlan = plan,
                ActionType = "Subscribe",
                Amount = 1200000,
                CreatedAt = new DateTime(2026, 1, 2)
            }
        };

        _mockContext.Setup(x => x.SubscriptionHistories).Returns(histories.AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(
            new GetGlobalPaymentHistoryQuery
            {
                EnterpriseSearch = "tc001",
                PageNumber = 1,
                PageSize = 10
            },
            CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items[0].EnterpriseName.Should().Be("Tech Corp");
    }

    [Fact]
    public async Task Handle_ShouldCombineAllFilters_AndComputeTotalPagesFromFilteredCount()
    {
        var plan = new SubscriptionPlan { Id = Guid.NewGuid(), PlanName = "Pro", PlanCode = "PRO" };
        var histories = new List<SubscriptionHistory>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseId = Guid.NewGuid(),
                Enterprise = new Enterprise { EnterpriseName = "Tech Corp", EnterpriseCode = "TC001" },
                SubscriptionPlanId = plan.Id,
                SubscriptionPlan = plan,
                ActionType = "Renew",
                Amount = 1000000,
                Currency = "VND",
                PaymentMethod = "BankTransfer",
                PaymentReference = "PAY-001",
                PeriodStartDate = new DateTime(2026, 2, 1),
                PeriodEndDate = new DateTime(2026, 3, 1),
                CreatedAt = new DateTime(2026, 2, 20)
            },
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseId = Guid.NewGuid(),
                Enterprise = new Enterprise { EnterpriseName = "Tech Corp", EnterpriseCode = "TC001" },
                SubscriptionPlanId = plan.Id,
                SubscriptionPlan = plan,
                ActionType = "Renew",
                Amount = 1200000,
                Currency = "VND",
                PaymentMethod = "Cash",
                PaymentReference = "PAY-002",
                PeriodStartDate = new DateTime(2026, 2, 1),
                PeriodEndDate = new DateTime(2026, 3, 1),
                CreatedAt = new DateTime(2026, 2, 21)
            },
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseId = Guid.NewGuid(),
                Enterprise = new Enterprise { EnterpriseName = "Retail Hub", EnterpriseCode = "RH001" },
                SubscriptionPlanId = plan.Id,
                SubscriptionPlan = plan,
                ActionType = "Upgrade",
                Amount = 1300000,
                Currency = "VND",
                PaymentMethod = "BankTransfer",
                PaymentReference = "PAY-003",
                PeriodStartDate = new DateTime(2026, 2, 1),
                PeriodEndDate = new DateTime(2026, 3, 1),
                CreatedAt = new DateTime(2026, 2, 22)
            }
        };

        _mockContext.Setup(x => x.SubscriptionHistories).Returns(histories.AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(
            new GetGlobalPaymentHistoryQuery
            {
                EnterpriseSearch = "tech",
                ActionType = "Renew",
                PaymentMethod = "BankTransfer",
                DateFrom = new DateTime(2026, 2, 20),
                DateTo = new DateTime(2026, 2, 20),
                PageNumber = 1,
                PageSize = 1
            },
            CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.TotalPages.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items[0].PaymentReference.Should().Be("PAY-001");
    }
}
