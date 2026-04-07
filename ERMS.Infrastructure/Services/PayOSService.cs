using ERMS.Application.Interface;
using ERMS.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;
using System.Text.Json;

namespace ERMS.Infrastructure.Services;

public class PayOSService : IPayOSService
{
    private readonly PayOSClient _payOS;
    private readonly string _returnUrl;
    private readonly string _cancelUrl;

    public PayOSService(IOptions<PayOSSettings> settings, IConfiguration configuration)
    {
        var s = settings.Value;
        _payOS = new PayOSClient(new PayOSOptions
        {
            ClientId = s.ClientId,
            ApiKey = s.ApiKey,
            ChecksumKey = s.ChecksumKey
        });

        var clientUrl = (configuration["ClientSettings:Url"] ?? "http://localhost:3000").TrimEnd('/');
        _returnUrl = $"{clientUrl}/enterprise/hr/subscription/result";
        _cancelUrl = $"{clientUrl}/enterprise/hr/subscription";
    }

    public async Task<CreatePaymentLinkResult> CreatePaymentLinkAsync(
        long orderCode,
        int amount,
        string description,
        string buyerName,
        string buyerEmail,
        int? expiredAtTimestamp = null)
    {
        var paymentData = new CreatePaymentLinkRequest
        {
            OrderCode = orderCode,
            Amount = amount,
            Description = description,
            Items = [],
            CancelUrl = _cancelUrl,
            ReturnUrl = _returnUrl,
            BuyerName = buyerName,
            BuyerEmail = buyerEmail,
            ExpiredAt = expiredAtTimestamp
        };

        var response = await _payOS.PaymentRequests.CreateAsync(paymentData);

        return new CreatePaymentLinkResult
        {
            CheckoutUrl = response.CheckoutUrl,
            PaymentLinkId = response.PaymentLinkId,
            QrCode = response.QrCode
        };
    }

    public async Task<PayOSWebhookData> VerifyWebhookAsync(string webhookBody)
    {
        var webhook = JsonSerializer.Deserialize<Webhook>(webhookBody)
            ?? throw new InvalidOperationException("Webhook body is invalid.");
        var webhookData = await _payOS.Webhooks.VerifyAsync(webhook);

        return new PayOSWebhookData
        {
            OrderCode = webhookData.OrderCode,
            Amount = (int)webhookData.Amount,
            Description = webhookData.Description ?? string.Empty,
            AccountNumber = webhookData.AccountNumber ?? string.Empty,
            Reference = webhookData.Reference ?? string.Empty,
            TransactionDateTime = webhookData.TransactionDateTime ?? string.Empty,
            Currency = webhookData.Currency ?? string.Empty,
            PaymentLinkId = webhookData.PaymentLinkId ?? string.Empty,
            Code = webhookData.Code ?? string.Empty,
            Desc = webhookData.Description2 ?? string.Empty
        };
    }

    public async Task CancelPaymentLinkAsync(long orderCode, string? reason = null)
    {
        await _payOS.PaymentRequests.CancelAsync(orderCode, reason ?? string.Empty);
    }
}
