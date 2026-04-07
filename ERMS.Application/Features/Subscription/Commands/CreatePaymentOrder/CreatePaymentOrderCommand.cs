using MediatR;

namespace ERMS.Application.Features.Subscription.Commands.CreatePaymentOrder;

public class CreatePaymentOrderCommand : IRequest<CreatePaymentOrderResponse>
{
    public Guid SubscriptionPlanId { get; set; }
}

public class CreatePaymentOrderResponse
{
    public Guid PaymentOrderId { get; set; }
    public long OrderCode { get; set; }
    public string CheckoutUrl { get; set; } = string.Empty;
    public string PaymentLinkId { get; set; } = string.Empty;
    public string QrCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
