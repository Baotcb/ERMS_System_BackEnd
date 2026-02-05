using ERMS.Application.Features.Enterprises.Commands.LockEnterprise;
using ERMS.Application.Features.Enterprises.Commands.ViewPaymentHistoryEnterprise;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ERMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableRateLimiting("fixed")]
    public class EnterpriseController : ControllerBase
    {
        private readonly ISender _sender;
        public EnterpriseController(ISender sender)
        {
            _sender = sender;
        }
        [HttpPost("lock-enterprise")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> LockEnterprise([FromBody] LockEnterpriseCommand command)
        {
            try
            {
                var result = await _sender.Send(command);
                return Ok(new
                {
                    message = command.IsLocked ? "Khóa doanh nghiệp thành công." : "Mở khóa doanh nghiệp thành công.",
                    result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpGet("PaymentHistoryEnterprise")]
        [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Director)]
        public async Task<IActionResult> ViewPaymentHistoryEnterprise([FromBody] ViewPaymentHistoryEnterpriseCommand command)
        {
            try
            {
                var result = await _sender.Send(command);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }

        }
    }
}
