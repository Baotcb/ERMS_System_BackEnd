namespace ERMS.Application.Interface;

public class CreatePaymentLinkResult
{
    public string CheckoutUrl { get; set; } = string.Empty;
    public string PaymentLinkId { get; set; } = string.Empty;
    public string QrCode { get; set; } = string.Empty;
}

public class PayOSWebhookData
{
    public long OrderCode { get; set; }
    public int Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string TransactionDateTime { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public string PaymentLinkId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Desc { get; set; } = string.Empty;
}

public interface IPayOSService
{
    Task<CreatePaymentLinkResult> CreatePaymentLinkAsync(
        long orderCode,
        int amount,
        string description,
        string buyerName,
        string buyerEmail,
        int? expiredAtTimestamp = null);

    Task<PayOSWebhookData> VerifyWebhookAsync(string webhookBody);

    Task CancelPaymentLinkAsync(long orderCode, string? reason = null);
}
