using ERMS.Application.Features.Subscription.Queries.GetSubscriptionPlans;
using ERMS.Domain.Entities.Enterprise;
using ERMS.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ERMS.UnitTests.Features.Subscription.Queries.GetSubscriptionPlans;

public class GetSubscriptionPlansHandlerTests : IDisposable
{
    private readonly ERMSDbContext _context;
    private readonly GetSubscriptionPlansHandler _handler;

    public GetSubscriptionPlansHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ERMSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ERMSDbContext(options);
        _handler = new GetSubscriptionPlansHandler(_context);
    }

    [Fact]
    public async Task Handle_ShouldReturnOnlyActiveAndNonDeletedPlans_OrderedByDisplayOrder()
    {
        // Arrange
        _context.SubscriptionPlans.AddRange(
            new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                PlanName = "Free",
                PlanCode = "FREE",
                IsActive = true,
                IsDeleted = false,
                DisplayOrder = 2,
                PriceMonthly = 0
            },
            new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                PlanName = "Pro",
                PlanCode = "PRO",
                IsActive = true,
                IsDeleted = false,
                DisplayOrder = 1,
                PriceMonthly = 10000
            },
            new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                PlanName = "Inactive",
                PlanCode = "INACTIVE",
                IsActive = false,
                IsDeleted = false,
                DisplayOrder = 3,
                PriceMonthly = 0
            },
            new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                PlanName = "Deleted",
                PlanCode = "DELETED",
                IsActive = true,
                IsDeleted = true,
                DisplayOrder = 4,
                PriceMonthly = 0
            });

        await _context.SaveChangesAsync();

        // Act
        var result = await _handler.Handle(new GetSubscriptionPlansQuery(), CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Select(p => p.PlanCode).Should().ContainInOrder("PRO", "FREE");
        result.Select(p => p.Price).Should().ContainInOrder(10000, 0);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
