using ERMS.Application.Interface;
using ERMS.Domain.Constants.Enterprise;
using ERMS.Domain.Entities.Enterprise;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.Subscription.Commands.HandlePayOSWebhook;

public class HandlePayOSWebhookHandler : IRequestHandler<HandlePayOSWebhookCommand, bool>
{
    private readonly IERMSDbContext _context;
    private readonly IPayOSService _payOSService;
    private readonly ILogger<HandlePayOSWebhookHandler> _logger;

    public HandlePayOSWebhookHandler(
        IERMSDbContext context,
        IPayOSService payOSService,
        ILogger<HandlePayOSWebhookHandler> logger)
    {
        _context = context;
        _payOSService = payOSService;
        _logger = logger;
    }

    public async Task<bool> Handle(HandlePayOSWebhookCommand request, CancellationToken cancellationToken)
    {
        PayOSWebhookData webhookData;
        try
        {
            webhookData = await _payOSService.VerifyWebhookAsync(request.WebhookBody);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to verify PayOS webhook signature or payload.");
            return false;
        }

        if (webhookData.Code != "00")
        {
            return true;
        }

        var paymentOrder = await _context.PaymentOrders
            .Include(p => p.Enterprise)
            .Include(p => p.SubscriptionPlan)
            .FirstOrDefaultAsync(p => p.OrderCode == webhookData.OrderCode, cancellationToken);

        if (paymentOrder == null)
        {
            return true;
        }

        if (paymentOrder.Status != PaymentOrderConstants.Status.Pending)
        {
            return true;
        }

        await using var transaction = await _context.BeginTransactionAsync(cancellationToken);
        try
        {
            var utcNow = DateTime.UtcNow;
            var enterprise = paymentOrder.Enterprise;
            var previousPlanId = enterprise.SubscriptionPlanId;

            paymentOrder.Status = PaymentOrderConstants.Status.Paid;
            paymentOrder.PaidAt = utcNow;
            paymentOrder.PayOSReference = webhookData.Reference;

            enterprise.SubscriptionPlanId = paymentOrder.SubscriptionPlanId;
            enterprise.SubscriptionStatus = "Active";

            var actionType = previousPlanId == paymentOrder.SubscriptionPlanId
                ? PaymentOrderConstants.ActionType.Renew
                : PaymentOrderConstants.ActionType.Upgrade;
            DateTime periodStart;
            DateTime periodEnd;

            if (actionType == PaymentOrderConstants.ActionType.Renew)
            {
                periodStart = enterprise.SubscriptionEndDate > utcNow ? enterprise.SubscriptionEndDate : utcNow;
                periodEnd = periodStart.AddMonths(3);
                enterprise.SubscriptionEndDate = periodEnd;
                if (enterprise.SubscriptionStartDate == default)
                {
                    enterprise.SubscriptionStartDate = periodStart;
                }
            }
            else
            {
                periodStart = utcNow;
                periodEnd = periodStart.AddMonths(3);
                enterprise.SubscriptionStartDate = periodStart;
                enterprise.SubscriptionEndDate = periodEnd;
            }

            var history = new SubscriptionHistory
            {
                Id = Guid.CreateVersion7(),
                EnterpriseId = paymentOrder.EnterpriseId,
                SubscriptionPlanId = paymentOrder.SubscriptionPlanId,
                PreviousPlanId = previousPlanId,
                ActionType = actionType,
                Amount = paymentOrder.Amount,
                Currency = "VND",
                PaymentMethod = "PayOS",
                PaymentReference = webhookData.Reference,
                PeriodStartDate = periodStart,
                PeriodEndDate = periodEnd,
                Note = $"PayOS OrderCode: {webhookData.OrderCode}",
                CreatedById = paymentOrder.CreatedById
            };

            _context.SubscriptionHistories.Add(history);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
