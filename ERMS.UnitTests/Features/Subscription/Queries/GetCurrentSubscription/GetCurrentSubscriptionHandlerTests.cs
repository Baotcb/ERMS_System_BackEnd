using ERMS.Application.Features.Subscription.Queries.GetCurrentSubscription;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Enterprise;
using ERMS.Domain.Entities.Enterprise;
using ERMS.Domain.Entities.Recruitment;
using ERMS.Domain.Entities.Training;
using ERMS.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ERMS.UnitTests.Features.Subscription.Queries.GetCurrentSubscription;

public class GetCurrentSubscriptionHandlerTests : IDisposable
{
    private readonly ERMSDbContext _context;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly GetCurrentSubscriptionHandler _handler;
    private readonly Guid _enterpriseId = Guid.NewGuid();

    public GetCurrentSubscriptionHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ERMSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ERMSDbContext(options);
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(_enterpriseId);

        _handler = new GetCurrentSubscriptionHandler(_context, _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnCurrentPlanUsageAndHasPendingPayment_WhenPendingOrderIsActive()
    {
        // Arrange
        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Pro Plan",
            PlanCode = "PRO",
            IsActive = true,
            IsDeleted = false,
            PriceMonthly = 10000,
            MaxJobPostings = 20,
            MaxCourses = 15
        };

        var enterprise = new Enterprise
        {
            Id = _enterpriseId,
            EnterpriseName = "Acme",
            EnterpriseCode = "ACME",
            SubscriptionPlanId = plan.Id,
            SubscriptionPlan = plan,
            SubscriptionStartDate = DateTime.UtcNow.AddDays(-10),
            SubscriptionEndDate = DateTime.UtcNow.AddDays(80),
            SubscriptionStatus = "Active",
            IsDeleted = false
        };

        _context.SubscriptionPlans.Add(plan);
        _context.Enterprises.Add(enterprise);
        _context.JobPostings.AddRange(
            new JobPosting
            {
                Id = Guid.NewGuid(),
                EnterpriseId = _enterpriseId,
                DepartmentId = 1,
                JobTitle = "SE",
                Description = "Desc",
                CreatedById = Guid.NewGuid(),
                Status = "Published",
                IsDeleted = false
            },
            new JobPosting
            {
                Id = Guid.NewGuid(),
                EnterpriseId = _enterpriseId,
                DepartmentId = 1,
                JobTitle = "QA",
                Description = "Desc",
                CreatedById = Guid.NewGuid(),
                Status = "Published",
                IsDeleted = false
            });

        _context.Courses.Add(new Course
        {
            Id = Guid.NewGuid(),
            EnterpriseId = _enterpriseId,
            CourseName = "C1",
            CourseCode = "C1",
            TrainerEmail = "t@acme.vn",
            Status = "Published",
            IsDeleted = false
        });

        _context.PaymentOrders.Add(new PaymentOrder
        {
            Id = Guid.NewGuid(),
            OrderCode = 12345,
            EnterpriseId = _enterpriseId,
            Enterprise = enterprise,
            SubscriptionPlanId = plan.Id,
            SubscriptionPlan = plan,
            PreviousPlanId = plan.Id,
            PreviousPlan = plan,
            Amount = 10000,
            Currency = "VND",
            Description = "test",
            Status = PaymentOrderConstants.Status.Pending,
            CreatedById = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        });

        await _context.SaveChangesAsync();

        // Act
        var result = await _handler.Handle(new GetCurrentSubscriptionQuery(), CancellationToken.None);

        // Assert
        result.EnterpriseId.Should().Be(_enterpriseId);
        result.CurrentPlan.PlanCode.Should().Be("PRO");
        result.Usage.CurrentJobPostings.Should().Be(2);
        result.Usage.CurrentCourses.Should().Be(1);
        result.HasPendingPayment.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldIgnoreExpiredPendingPayment_WhenOlderThanThirtyMinutes()
    {
        // Arrange
        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Free Plan",
            PlanCode = "FREE",
            IsActive = true,
            IsDeleted = false,
            PriceMonthly = 0,
            MaxJobPostings = 2,
            MaxCourses = 2
        };

        var enterprise = new Enterprise
        {
            Id = _enterpriseId,
            EnterpriseName = "Acme",
            EnterpriseCode = "ACME",
            SubscriptionPlanId = plan.Id,
            SubscriptionPlan = plan,
            SubscriptionStartDate = DateTime.UtcNow.AddDays(-30),
            SubscriptionEndDate = DateTime.UtcNow.AddDays(60),
            SubscriptionStatus = "Active",
            IsDeleted = false
        };

        _context.SubscriptionPlans.Add(plan);
        _context.Enterprises.Add(enterprise);
        _context.PaymentOrders.Add(new PaymentOrder
        {
            Id = Guid.NewGuid(),
            OrderCode = 22222,
            EnterpriseId = _enterpriseId,
            Enterprise = enterprise,
            SubscriptionPlanId = plan.Id,
            SubscriptionPlan = plan,
            Amount = 0,
            Currency = "VND",
            Description = "expired pending",
            Status = PaymentOrderConstants.Status.Pending,
            CreatedById = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow.AddMinutes(-40)
        });

        await _context.SaveChangesAsync();

        // Act
        var result = await _handler.Handle(new GetCurrentSubscriptionQuery(), CancellationToken.None);

        // Assert
        result.HasPendingPayment.Should().BeFalse();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
