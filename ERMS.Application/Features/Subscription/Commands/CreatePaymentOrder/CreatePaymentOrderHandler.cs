using ERMS.Application.Interface;
using ERMS.Domain.Constants.Enterprise;
using ERMS.Domain.Entities.Enterprise;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.Subscription.Commands.CreatePaymentOrder;

public class CreatePaymentOrderHandler : IRequestHandler<CreatePaymentOrderCommand, CreatePaymentOrderResponse>
{
    private static readonly TimeSpan PendingOrderTtl = TimeSpan.FromMinutes(30);
    private const int OrderCodeRandomRange = 1000;
    private const int OrderCodeGenerationMaxAttempts = 30;
    private const int SaveRetryMaxAttempts = 3;

    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPayOSService _payOSService;
    private readonly ILogger<CreatePaymentOrderHandler> _logger;

    public CreatePaymentOrderHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        IPayOSService payOSService,
        ILogger<CreatePaymentOrderHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _payOSService = payOSService;
        _logger = logger;
    }

    public async Task<CreatePaymentOrderResponse> Handle(CreatePaymentOrderCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Người dùng chưa xác thực");

        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync()
            ?? throw new UnauthorizedAccessException("Người dùng không liên kết với doanh nghiệp nào");

        var enterprise = await _context.Enterprises
            .Include(e => e.SubscriptionPlan)
            .FirstOrDefaultAsync(e => e.Id == enterpriseId && !e.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy doanh nghiệp");

        var targetPlan = await _context.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.Id == request.SubscriptionPlanId && p.IsActive && !p.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy gói đăng ký");

        if (targetPlan.PriceMonthly <= 0)
        {
            throw new InvalidOperationException("Không thể mua gói miễn phí");
        }

        await ExpireStalePendingOrdersAsync(enterpriseId, cancellationToken);

        var activePendingCutoff = DateTime.UtcNow.Subtract(PendingOrderTtl);
        var hasPending = await _context.PaymentOrders.AnyAsync(
            p => p.EnterpriseId == enterpriseId
                 && p.Status == PaymentOrderConstants.Status.Pending
                 && p.CreatedAt >= activePendingCutoff,
            cancellationToken);
        if (hasPending)
        {
            throw new InvalidOperationException("Đã có đơn thanh toán đang chờ xử lý. Vui lòng hoàn tất hoặc hủy trước.");
        }

        var amount = targetPlan.PriceMonthly;
        var orderCode = await GenerateOrderCodeAsync(cancellationToken);

        var paymentOrder = new PaymentOrder
        {
            Id = Guid.CreateVersion7(),
            OrderCode = orderCode,
            EnterpriseId = enterpriseId,
            SubscriptionPlanId = targetPlan.Id,
            PreviousPlanId = enterprise.SubscriptionPlanId,
            Amount = amount,
            Currency = "VND",
            Description = $"ERMS-{targetPlan.PlanCode}-QTR",
            Status = PaymentOrderConstants.Status.Pending,
            CreatedById = userId
        };

        var expiredAt = (int)DateTimeOffset.UtcNow.Add(PendingOrderTtl).ToUnixTimeSeconds();
        var buyerEmail = enterprise.Email ?? _currentUserService.Email ?? "customer@erms.vn";
        var payOSResult = await _payOSService.CreatePaymentLinkAsync(
            orderCode: orderCode,
            amount: (int)Math.Round(amount, MidpointRounding.AwayFromZero),
            description: paymentOrder.Description,
            buyerName: enterprise.EnterpriseName,
            buyerEmail: buyerEmail,
            expiredAtTimestamp: expiredAt);

        paymentOrder.PaymentLinkId = payOSResult.PaymentLinkId;
        paymentOrder.CheckoutUrl = payOSResult.CheckoutUrl;

        _context.PaymentOrders.Add(paymentOrder);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Không thể lưu đơn thanh toán với mã đơn {OrderCode}, có thể bị trùng lặp", orderCode);
            throw new InvalidOperationException("Không thể tạo đơn thanh toán. Vui lòng thử lại.", ex);
        }

        return new CreatePaymentOrderResponse
        {
            PaymentOrderId = paymentOrder.Id,
            OrderCode = paymentOrder.OrderCode,
            CheckoutUrl = payOSResult.CheckoutUrl,
            PaymentLinkId = payOSResult.PaymentLinkId,
            QrCode = payOSResult.QrCode,
            Amount = paymentOrder.Amount
        };
    }

    private async Task ExpireStalePendingOrdersAsync(Guid enterpriseId, CancellationToken cancellationToken)
    {
        var staleCutoff = DateTime.UtcNow.Subtract(PendingOrderTtl);
        var staleOrders = await _context.PaymentOrders
            .Where(p =>
                p.EnterpriseId == enterpriseId
                && p.Status == PaymentOrderConstants.Status.Pending
                && p.CreatedAt < staleCutoff)
            .ToListAsync(cancellationToken);

        if (staleOrders.Count == 0)
        {
            return;
        }

        var hasChanges = false;
        foreach (var staleOrder in staleOrders)
        {
            try
            {
                await _payOSService.CancelPaymentLinkAsync(staleOrder.OrderCode, "Expired due to timeout");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Không thể hủy liên kết thanh toán PayOS hết hạn cho mã đơn {OrderCode}. Giữ trạng thái chờ xử lý.",
                    staleOrder.OrderCode);
                continue;
            }

            var utcNow = DateTime.UtcNow;
            staleOrder.Status = PaymentOrderConstants.Status.Expired;
            staleOrder.CancelledAt = utcNow;
            staleOrder.UpdatedAt = utcNow;
            hasChanges = true;
        }

        if (hasChanges)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<long> GenerateOrderCodeAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < OrderCodeGenerationMaxAttempts; attempt++)
        {
            var value = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * OrderCodeRandomRange
                + Random.Shared.Next(0, OrderCodeRandomRange);
            var exists = await _context.PaymentOrders.AnyAsync(x => x.OrderCode == value, cancellationToken);
            if (!exists)
            {
                return value;
            }
        }

        throw new InvalidOperationException("Không thể tạo mã đơn hàng duy nhất");
    }
}
