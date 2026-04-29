using ERMS.Application.Features.Subscription.Commands.CreatePaymentOrder;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Enterprise;
using ERMS.Domain.Entities.Enterprise;
using ERMS.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace ERMS.UnitTests.Features.Subscription.Commands.CreatePaymentOrder;

public class CreatePaymentOrderHandlerTests : IDisposable
{
    private readonly ERMSDbContext _context;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IPayOSService> _payOSServiceMock;
    private readonly Mock<ILogger<CreatePaymentOrderHandler>> _loggerMock;
    private readonly CreatePaymentOrderHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _enterpriseId = Guid.NewGuid();

    public CreatePaymentOrderHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ERMSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ERMSDbContext(options);
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _payOSServiceMock = new Mock<IPayOSService>();
        _loggerMock = new Mock<ILogger<CreatePaymentOrderHandler>>();

        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(_enterpriseId);
        _currentUserServiceMock.Setup(x => x.Email).Returns("hr@acme.vn");

        _payOSServiceMock.Setup(x => x.CreatePaymentLinkAsync(
                It.IsAny<long>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int?>()))
            .ReturnsAsync(new CreatePaymentLinkResult
            {
                CheckoutUrl = "https://payos.vn/checkout/abc",
                PaymentLinkId = "plink_123",
                QrCode = "qr_123"
            });

        _handler = new CreatePaymentOrderHandler(
            _context,
            _currentUserServiceMock.Object,
            _payOSServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldCreatePaymentOrder_WhenTargetPlanIsPaid()
    {
        // Arrange
        var free = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Free",
            PlanCode = "FREE",
            IsActive = true,
            IsDeleted = false,
            PriceMonthly = 0
        };
        var pro = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Pro",
            PlanCode = "PRO",
            IsActive = true,
            IsDeleted = false,
            PriceMonthly = 10000
        };
        var enterprise = new Enterprise
        {
            Id = _enterpriseId,
            EnterpriseName = "Acme",
            EnterpriseCode = "ACME",
            Email = "billing@acme.vn",
            SubscriptionPlanId = free.Id,
            SubscriptionPlan = free,
            SubscriptionStartDate = DateTime.UtcNow.AddDays(-10),
            SubscriptionEndDate = DateTime.UtcNow.AddDays(80),
            SubscriptionStatus = "Active",
            IsDeleted = false
        };

        _context.SubscriptionPlans.AddRange(free, pro);
        _context.Enterprises.Add(enterprise);
        await _context.SaveChangesAsync();

        // Act
        var result = await _handler.Handle(new CreatePaymentOrderCommand { SubscriptionPlanId = pro.Id }, CancellationToken.None);

        // Assert
        result.PaymentOrderId.Should().NotBe(Guid.Empty);
        result.Amount.Should().Be(10000);
        result.PaymentLinkId.Should().Be("plink_123");
        result.CheckoutUrl.Should().Be("https://payos.vn/checkout/abc");
        (await _context.PaymentOrders.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldAllowRenew_WhenTargetPlanIsSameAsCurrentPlan()
    {
        // Arrange
        var pro = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Pro",
            PlanCode = "PRO",
            IsActive = true,
            IsDeleted = false,
            PriceMonthly = 10000
        };
        var enterprise = new Enterprise
        {
            Id = _enterpriseId,
            EnterpriseName = "Acme",
            EnterpriseCode = "ACME",
            SubscriptionPlanId = pro.Id,
            SubscriptionPlan = pro,
            SubscriptionStartDate = DateTime.UtcNow.AddDays(-5),
            SubscriptionEndDate = DateTime.UtcNow.AddDays(85),
            SubscriptionStatus = "Active",
            IsDeleted = false
        };

        _context.SubscriptionPlans.Add(pro);
        _context.Enterprises.Add(enterprise);
        await _context.SaveChangesAsync();

        // Act
        var result = await _handler.Handle(new CreatePaymentOrderCommand { SubscriptionPlanId = pro.Id }, CancellationToken.None);

        // Assert
        result.PaymentOrderId.Should().NotBe(Guid.Empty);
        (await _context.PaymentOrders.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenTargetPlanIsFree()
    {
        // Arrange
        var free = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Free",
            PlanCode = "FREE",
            IsActive = true,
            IsDeleted = false,
            PriceMonthly = 0
        };
        var pro = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Pro",
            PlanCode = "PRO",
            IsActive = true,
            IsDeleted = false,
            PriceMonthly = 10000
        };
        var enterprise = new Enterprise
        {
            Id = _enterpriseId,
            EnterpriseName = "Acme",
            EnterpriseCode = "ACME",
            SubscriptionPlanId = pro.Id,
            SubscriptionPlan = pro,
            SubscriptionStartDate = DateTime.UtcNow.AddDays(-5),
            SubscriptionEndDate = DateTime.UtcNow.AddDays(85),
            SubscriptionStatus = "Active",
            IsDeleted = false
        };

        _context.SubscriptionPlans.AddRange(free, pro);
        _context.Enterprises.Add(enterprise);
        await _context.SaveChangesAsync();

        // Act
        var act = () => _handler.Handle(new CreatePaymentOrderCommand { SubscriptionPlanId = free.Id }, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Không thể mua gói miễn phí*");
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenActivePendingPaymentExists()
    {
        // Arrange
        var free = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Free",
            PlanCode = "FREE",
            IsActive = true,
            IsDeleted = false,
            PriceMonthly = 0
        };
        var pro = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Pro",
            PlanCode = "PRO",
            IsActive = true,
            IsDeleted = false,
            PriceMonthly = 10000
        };
        var enterprise = new Enterprise
        {
            Id = _enterpriseId,
            EnterpriseName = "Acme",
            EnterpriseCode = "ACME",
            SubscriptionPlanId = free.Id,
            SubscriptionPlan = free,
            SubscriptionStartDate = DateTime.UtcNow.AddDays(-5),
            SubscriptionEndDate = DateTime.UtcNow.AddDays(85),
            SubscriptionStatus = "Active",
            IsDeleted = false
        };

        _context.SubscriptionPlans.AddRange(free, pro);
        _context.Enterprises.Add(enterprise);
        _context.PaymentOrders.Add(new PaymentOrder
        {
            Id = Guid.NewGuid(),
            OrderCode = 11111,
            EnterpriseId = _enterpriseId,
            SubscriptionPlanId = pro.Id,
            PreviousPlanId = free.Id,
            Amount = 10000,
            Description = "pending",
            Status = PaymentOrderConstants.Status.Pending,
            CreatedById = _userId,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        });
        await _context.SaveChangesAsync();

        // Act
        var act = () => _handler.Handle(new CreatePaymentOrderCommand { SubscriptionPlanId = pro.Id }, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Đã có đơn thanh toán đang chờ xử lý*");
    }

    [Fact]
    public async Task Handle_ShouldExpireOldPendingOrderAndCreateNewOne_WhenPendingOrderIsStale()
    {
        // Arrange
        var free = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Free",
            PlanCode = "FREE",
            IsActive = true,
            IsDeleted = false,
            PriceMonthly = 0
        };
        var pro = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Pro",
            PlanCode = "PRO",
            IsActive = true,
            IsDeleted = false,
            PriceMonthly = 10000
        };
        var enterprise = new Enterprise
        {
            Id = _enterpriseId,
            EnterpriseName = "Acme",
            EnterpriseCode = "ACME",
            SubscriptionPlanId = free.Id,
            SubscriptionPlan = free,
            SubscriptionStartDate = DateTime.UtcNow.AddDays(-5),
            SubscriptionEndDate = DateTime.UtcNow.AddDays(85),
            SubscriptionStatus = "Active",
            IsDeleted = false
        };

        var staleOrder = new PaymentOrder
        {
            Id = Guid.NewGuid(),
            OrderCode = 11111,
            EnterpriseId = _enterpriseId,
            SubscriptionPlanId = pro.Id,
            PreviousPlanId = free.Id,
            Amount = 10000,
            Description = "stale pending",
            Status = PaymentOrderConstants.Status.Pending,
            CreatedById = _userId,
            CreatedAt = DateTime.UtcNow.AddMinutes(-45)
        };

        _context.SubscriptionPlans.AddRange(free, pro);
        _context.Enterprises.Add(enterprise);
        _context.PaymentOrders.Add(staleOrder);
        await _context.SaveChangesAsync();

        // Act
        var result = await _handler.Handle(new CreatePaymentOrderCommand { SubscriptionPlanId = pro.Id }, CancellationToken.None);

        // Assert
        result.PaymentOrderId.Should().NotBe(Guid.Empty);
        var staleInDb = await _context.PaymentOrders.FirstAsync(x => x.Id == staleOrder.Id);
        staleInDb.Status.Should().Be(PaymentOrderConstants.Status.Expired);
        staleInDb.CancelledAt.Should().NotBeNull();
        _payOSServiceMock.Verify(
            x => x.CancelPaymentLinkAsync(staleOrder.OrderCode, It.IsAny<string?>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldKeepStaleOrderPending_WhenCancelPayOSLinkFails()
    {
        // Arrange
        var free = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Free",
            PlanCode = "FREE",
            IsActive = true,
            IsDeleted = false,
            PriceMonthly = 0
        };
        var pro = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Pro",
            PlanCode = "PRO",
            IsActive = true,
            IsDeleted = false,
            PriceMonthly = 10000
        };
        var enterprise = new Enterprise
        {
            Id = _enterpriseId,
            EnterpriseName = "Acme",
            EnterpriseCode = "ACME",
            SubscriptionPlanId = free.Id,
            SubscriptionPlan = free,
            SubscriptionStartDate = DateTime.UtcNow.AddDays(-5),
            SubscriptionEndDate = DateTime.UtcNow.AddDays(85),
            SubscriptionStatus = "Active",
            IsDeleted = false
        };

        var staleOrder = new PaymentOrder
        {
            Id = Guid.NewGuid(),
            OrderCode = 22222,
            EnterpriseId = _enterpriseId,
            SubscriptionPlanId = pro.Id,
            PreviousPlanId = free.Id,
            Amount = 10000,
            Description = "stale pending",
            Status = PaymentOrderConstants.Status.Pending,
            CreatedById = _userId,
            CreatedAt = DateTime.UtcNow.AddMinutes(-45)
        };

        _payOSServiceMock
            .Setup(x => x.CancelPaymentLinkAsync(staleOrder.OrderCode, It.IsAny<string?>()))
            .ThrowsAsync(new InvalidOperationException("payos timeout"));

        _context.SubscriptionPlans.AddRange(free, pro);
        _context.Enterprises.Add(enterprise);
        _context.PaymentOrders.Add(staleOrder);
        await _context.SaveChangesAsync();

        // Act
        var result = await _handler.Handle(new CreatePaymentOrderCommand { SubscriptionPlanId = pro.Id }, CancellationToken.None);

        // Assert
        result.PaymentOrderId.Should().NotBe(Guid.Empty);
        var staleInDb = await _context.PaymentOrders.FirstAsync(x => x.Id == staleOrder.Id);
        staleInDb.Status.Should().Be(PaymentOrderConstants.Status.Pending);
        staleInDb.CancelledAt.Should().BeNull();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
