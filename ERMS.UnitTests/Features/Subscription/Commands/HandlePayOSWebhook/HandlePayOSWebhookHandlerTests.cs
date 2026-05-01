using ERMS.Application.Features.Subscription.Commands.HandlePayOSWebhook;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Enterprise;
using ERMS.Domain.Entities.Enterprise;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;

namespace ERMS.UnitTests.Features.Subscription.Commands.HandlePayOSWebhook;

public class HandlePayOSWebhookHandlerTests
{
    private readonly Mock<IERMSDbContext> _contextMock;
    private readonly Mock<IPayOSService> _payOSServiceMock;
    private readonly Mock<ILogger<HandlePayOSWebhookHandler>> _loggerMock;
    private readonly HandlePayOSWebhookHandler _handler;

    public HandlePayOSWebhookHandlerTests()
    {
        _contextMock = new Mock<IERMSDbContext>();
        _payOSServiceMock = new Mock<IPayOSService>();
        _loggerMock = new Mock<ILogger<HandlePayOSWebhookHandler>>();
        _handler = new HandlePayOSWebhookHandler(_contextMock.Object, _payOSServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldUpdatePaymentAndSubscription_ForSuccessfulRenew()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var plan = new SubscriptionPlan
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
            Id = Guid.NewGuid(),
            EnterpriseName = "Acme",
            EnterpriseCode = "ACME",
            SubscriptionPlanId = plan.Id,
            SubscriptionPlan = plan,
            SubscriptionStartDate = now.AddMonths(-2),
            SubscriptionEndDate = now.AddMonths(1), // still active
            SubscriptionStatus = "Active",
            IsDeleted = false
        };
        var paymentOrder = new PaymentOrder
        {
            Id = Guid.NewGuid(),
            OrderCode = 999001,
            EnterpriseId = enterprise.Id,
            Enterprise = enterprise,
            SubscriptionPlanId = plan.Id, // same plan => Renew
            SubscriptionPlan = plan,
            PreviousPlanId = plan.Id,
            PreviousPlan = plan,
            Amount = 10000,
            Description = "renew",
            Status = PaymentOrderConstants.Status.Pending,
            CreatedById = Guid.NewGuid(),
            CreatedAt = now.AddMinutes(-3)
        };

        var paymentOrders = new List<PaymentOrder> { paymentOrder }.AsQueryable();
        var paymentOrdersSet = paymentOrders.BuildMockDbSet();
        _contextMock.Setup(x => x.PaymentOrders).Returns(paymentOrdersSet.Object);

        var subscriptionHistories = new List<SubscriptionHistory>().AsQueryable();
        var historySet = subscriptionHistories.BuildMockDbSet();
        _contextMock.Setup(x => x.SubscriptionHistories).Returns(historySet.Object);

        _contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var transactionMock = new Mock<IDbContextTransaction>();
        transactionMock.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        transactionMock.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _contextMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionMock.Object);

        _payOSServiceMock.Setup(x => x.VerifyWebhookAsync(It.IsAny<string>()))
            .ReturnsAsync(new PayOSWebhookData
            {
                OrderCode = 999001,
                Amount = 10000,
                Code = "00",
                Reference = "TXN-001"
            });

        // Act
        var result = await _handler.Handle(new HandlePayOSWebhookCommand { WebhookBody = "{...}" }, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        paymentOrder.Status.Should().Be(PaymentOrderConstants.Status.Paid);
        paymentOrder.PayOSReference.Should().Be("TXN-001");
        enterprise.SubscriptionEndDate.Should().BeAfter(now.AddMonths(3)); // renew from existing end date

        historySet.Verify(x => x.Add(It.Is<SubscriptionHistory>(h =>
            h.ActionType == "Renew" &&
            h.PaymentReference == "TXN-001" &&
            h.Currency == "VND")), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnTrueWithoutChanges_WhenPaymentAlreadyProcessed()
    {
        // Arrange
        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Pro",
            PlanCode = "PRO",
            IsActive = true,
            IsDeleted = false
        };
        var enterprise = new Enterprise
        {
            Id = Guid.NewGuid(),
            EnterpriseName = "Acme",
            EnterpriseCode = "ACME",
            SubscriptionPlanId = plan.Id,
            SubscriptionPlan = plan,
            SubscriptionStartDate = DateTime.UtcNow.AddMonths(-1),
            SubscriptionEndDate = DateTime.UtcNow.AddMonths(2),
            SubscriptionStatus = "Active",
            IsDeleted = false
        };
        var paymentOrder = new PaymentOrder
        {
            Id = Guid.NewGuid(),
            OrderCode = 999002,
            EnterpriseId = enterprise.Id,
            Enterprise = enterprise,
            SubscriptionPlanId = plan.Id,
            SubscriptionPlan = plan,
            Amount = 10000,
            Description = "paid",
            Status = PaymentOrderConstants.Status.Paid,
            CreatedById = Guid.NewGuid()
        };

        var paymentOrders = new List<PaymentOrder> { paymentOrder }.AsQueryable();
        var paymentOrdersSet = paymentOrders.BuildMockDbSet();
        _contextMock.Setup(x => x.PaymentOrders).Returns(paymentOrdersSet.Object);

        var historySet = new List<SubscriptionHistory>().AsQueryable().BuildMockDbSet();
        _contextMock.Setup(x => x.SubscriptionHistories).Returns(historySet.Object);

        _payOSServiceMock.Setup(x => x.VerifyWebhookAsync(It.IsAny<string>()))
            .ReturnsAsync(new PayOSWebhookData
            {
                OrderCode = 999002,
                Amount = 10000,
                Code = "00",
                Reference = "TXN-002"
            });

        // Act
        var result = await _handler.Handle(new HandlePayOSWebhookCommand { WebhookBody = "{...}" }, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        historySet.Verify(x => x.Add(It.IsAny<SubscriptionHistory>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnFalse_WhenWebhookSignatureInvalid()
    {
        // Arrange
        _payOSServiceMock.Setup(x => x.VerifyWebhookAsync(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Invalid signature"));

        // Act
        var result = await _handler.Handle(new HandlePayOSWebhookCommand { WebhookBody = "{...}" }, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Không thể xác minh chữ ký hoặc dữ liệu webhook PayOS.")),
                It.IsAny<InvalidOperationException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldUpgradeSubscription_WhenDifferentPlan()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var freePlan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Free",
            PlanCode = "FREE",
            IsActive = true,
            IsDeleted = false,
            PriceMonthly = 0
        };
        var proPlan = new SubscriptionPlan
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
            Id = Guid.NewGuid(),
            EnterpriseName = "Acme",
            EnterpriseCode = "ACME",
            SubscriptionPlanId = freePlan.Id,
            SubscriptionPlan = freePlan,
            SubscriptionStartDate = now.AddMonths(-6),
            SubscriptionEndDate = now.AddMonths(-3), // expired free
            SubscriptionStatus = "Active",
            IsDeleted = false
        };
        var paymentOrder = new PaymentOrder
        {
            Id = Guid.NewGuid(),
            OrderCode = 999003,
            EnterpriseId = enterprise.Id,
            Enterprise = enterprise,
            SubscriptionPlanId = proPlan.Id, // different plan => Upgrade
            SubscriptionPlan = proPlan,
            PreviousPlanId = freePlan.Id,
            PreviousPlan = freePlan,
            Amount = 10000,
            Description = "upgrade",
            Status = PaymentOrderConstants.Status.Pending,
            CreatedById = Guid.NewGuid(),
            CreatedAt = now.AddMinutes(-3)
        };

        var paymentOrders = new List<PaymentOrder> { paymentOrder }.AsQueryable();
        var paymentOrdersSet = paymentOrders.BuildMockDbSet();
        _contextMock.Setup(x => x.PaymentOrders).Returns(paymentOrdersSet.Object);

        var subscriptionHistories = new List<SubscriptionHistory>().AsQueryable();
        var historySet = subscriptionHistories.BuildMockDbSet();
        _contextMock.Setup(x => x.SubscriptionHistories).Returns(historySet.Object);

        _contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var transactionMock = new Mock<IDbContextTransaction>();
        transactionMock.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        transactionMock.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _contextMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionMock.Object);

        _payOSServiceMock.Setup(x => x.VerifyWebhookAsync(It.IsAny<string>()))
            .ReturnsAsync(new PayOSWebhookData
            {
                OrderCode = 999003,
                Amount = 10000,
                Code = "00",
                Reference = "TXN-003"
            });

        // Act
        var result = await _handler.Handle(new HandlePayOSWebhookCommand { WebhookBody = "{...}" }, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        paymentOrder.Status.Should().Be(PaymentOrderConstants.Status.Paid);
        paymentOrder.PayOSReference.Should().Be("TXN-003");

        // Upgrade should reset dates from now, not extend from existing end date
        enterprise.SubscriptionPlanId.Should().Be(proPlan.Id);
        enterprise.SubscriptionStartDate.Should().BeCloseTo(now, TimeSpan.FromSeconds(5));
        enterprise.SubscriptionEndDate.Should().BeCloseTo(now.AddMonths(3), TimeSpan.FromSeconds(5));

        historySet.Verify(x => x.Add(It.Is<SubscriptionHistory>(h =>
            h.ActionType == "Upgrade" &&
            h.SubscriptionPlanId == proPlan.Id &&
            h.PreviousPlanId == freePlan.Id &&
            h.PaymentReference == "TXN-003" &&
            h.Currency == "VND")), Times.Once);
    }
}
