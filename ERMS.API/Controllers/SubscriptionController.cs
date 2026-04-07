using ERMS.Application.Features.Subscription.Commands.CreatePaymentOrder;
using ERMS.Application.Features.Subscription.Commands.HandlePayOSWebhook;
using ERMS.Application.Features.Subscription.Queries.GetCurrentSubscription;
using ERMS.Application.Features.Subscription.Queries.GetSubscriptionPlans;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ERMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("fixed")]
public class SubscriptionController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<SubscriptionController> _logger;

    public SubscriptionController(IMediator mediator, ILogger<SubscriptionController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet("plans")]
    [Authorize(Roles = AppRoles.HRManager)]
    public async Task<IActionResult> GetPlans()
    {
        var result = await _mediator.Send(new GetSubscriptionPlansQuery());
        return Ok(result);
    }

    [HttpGet("current")]
    [Authorize(Roles = AppRoles.HRManager)]
    public async Task<IActionResult> GetCurrentSubscription()
    {
        var result = await _mediator.Send(new GetCurrentSubscriptionQuery());
        return Ok(result);
    }

    [HttpPost("create-payment")]
    [Authorize(Roles = AppRoles.HRManager)]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentOrderCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    [DisableRateLimiting]
    [RequestSizeLimit(64 * 1024)]
    public async Task<IActionResult> PayOSWebhook()
    {
        try
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            var result = await _mediator.Send(new HandlePayOSWebhookCommand { WebhookBody = body });
            return Ok(new { success = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error processing PayOS webhook");
            return Ok(new { success = false });
        }
    }

    [HttpGet("webhook")]
    [AllowAnonymous]
    public IActionResult PayOSWebhookVerify()
    {
        return Ok(new { success = true });
    }
}
