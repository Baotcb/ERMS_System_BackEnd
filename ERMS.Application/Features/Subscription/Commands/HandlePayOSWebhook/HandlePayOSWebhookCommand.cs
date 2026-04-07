using MediatR;

namespace ERMS.Application.Features.Subscription.Commands.HandlePayOSWebhook;

public class HandlePayOSWebhookCommand : IRequest<bool>
{
    public string WebhookBody { get; set; } = string.Empty;
}
