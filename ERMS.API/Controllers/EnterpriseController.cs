using ERMS.Application.Features.Enterprises.Commands.ViewPaymentHistoryEnterprise;
using ERMS.Application.Features.Enterprises.Commands.GetUrlAvataEnterprise;
using ERMS.Application.Features.Enterprises.Queries.GetEnterpriseDetails;
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

        [HttpGet("get-url-avata-enterprise")]
        [Authorize(Roles =AppRoles.Employee+","+AppRoles.HRManager+","+AppRoles.Trainer+","+AppRoles.DepartmentHead+","+AppRoles.Director)]
        public async Task<IActionResult> GetUrlAvataEnterprise()
        {
            try
            {
                var result = await _sender.Send(new GetUrlAvataEnterpriseCommand());
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
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

        /// <summary>
        /// Get public details of an Enterprise
        /// </summary>
        /// <remarks>
        /// **Access:** Public (AllowAnonymous)
        /// 
        /// Retrieves a filtered, public-facing profile of an Enterprise, 
        /// primarily intended for candidates viewing company information.
        /// Excludes sensitive details such as tax codes and subscription status.
        /// </remarks>
        /// <param name="id">The unique identifier of the Enterprise</param>
        /// <returns>Public enterprise details including name, contact info, and logo</returns>
        [HttpGet("{id}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(GetEnterpriseDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEnterpriseDetails(Guid id)
        {
            try
            {
                var query = new GetEnterpriseDetailsQuery { Id = id };
                var result = await _sender.Send(query);
                return Ok(result);
            }
            catch (Exception ex) when (ex.Message.Contains("not found") || ex.Message.Contains("unavailable"))
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
