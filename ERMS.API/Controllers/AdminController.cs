using ERMS.Application.Features.Admin.Commands.SetEnterpriseLockState;
using ERMS.Application.Features.Admin.Commands.SetEnterpriseStatus;
using ERMS.Application.Features.Admin.Queries.GetAdminDashboard;
using ERMS.Application.Features.Admin.Queries.GetAiServiceOverview;
using ERMS.Application.Features.Admin.Queries.GetEnterpriseAdminDetail;
using ERMS.Application.Features.Admin.Queries.GetEnterpriseList;
using ERMS.Application.Features.Admin.Queries.GetGlobalPaymentHistory;
using ERMS.Application.Features.Admin.Queries.GetPlatformStats;
using ERMS.Application.Features.Admin.Queries.GetSystemIntegrations;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ERMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableRateLimiting("admin-fixed")]
    [Authorize(Roles = AppRoles.Admin)]
    public class AdminController : ControllerBase
    {
        private readonly ISender _sender;

        public AdminController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var result = await _sender.Send(new GetAdminDashboardQuery());
            return Ok(result);
        }

        [HttpGet("enterprises")]
        public async Task<IActionResult> GetEnterprises([FromQuery] GetEnterpriseListQuery query)
        {
            var result = await _sender.Send(query);
            return Ok(result);
        }

        [HttpGet("enterprise/{enterpriseId:guid}")]
        public async Task<IActionResult> GetEnterpriseDetailById(Guid enterpriseId)
        {
            var result = await _sender.Send(new GetEnterpriseAdminDetailQuery { EnterpriseId = enterpriseId });
            return Ok(result);
        }

        [HttpPut("enterprise/{enterpriseId:guid}/status")]
        public async Task<IActionResult> SetEnterpriseStatus(Guid enterpriseId, [FromBody] SetEnterpriseStatusCommand command)
        {
            command.EnterpriseId = enterpriseId;

            var result = await _sender.Send(command);
            return Ok(new
            {
                message = "Cập nhật trạng thái doanh nghiệp thành công.",
                result
            });
        }

        [HttpPut("enterprise-lock-state")]
        public async Task<IActionResult> SetEnterpriseLockState([FromBody] SetEnterpriseLockStateCommand command)
        {
            var result = await _sender.Send(command);
            return Ok(new
            {
                message = command.IsLocked ? "Khóa doanh nghiệp thành công." : "Mở khóa doanh nghiệp thành công.",
                result
            });
        }

        [HttpGet("payment-history")]
        public async Task<IActionResult> GetPaymentHistory([FromQuery] GetGlobalPaymentHistoryQuery query)
        {
            var result = await _sender.Send(query);
            return Ok(result);
        }

        [HttpGet("platform-stats")]
        public async Task<IActionResult> GetPlatformStats()
        {
            var result = await _sender.Send(new GetPlatformStatsQuery());
            return Ok(result);
        }

        [HttpGet("ai-services")]
        public async Task<IActionResult> GetAiServices()
        {
            var result = await _sender.Send(new GetAiServiceOverviewQuery());
            return Ok(result);
        }

        [HttpGet("system-integrations")]
        [HttpGet("integrations")]
        public async Task<IActionResult> GetSystemIntegrations()
        {
            var result = await _sender.Send(new GetSystemIntegrationsQuery());
            return Ok(result);
        }
    }
}
