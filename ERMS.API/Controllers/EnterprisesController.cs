using ERMS.Application.Features.Enterprises.Commands.UpdateEnterprise;

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace ERMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EnterprisesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public EnterprisesController(IMediator mediator)
        {
            _mediator = mediator;
        }



        /// <summary>
        /// Cập nhật thông tin doanh nghiệp (HR only)
        /// Chỉ cho phép cập nhật: EnterpriseName, Address, Phone, Website
        /// Mỗi lần cập nhật phải cách nhau ít nhất 6 tháng
        /// </summary>
        [HttpPut]
        public async Task<IActionResult> UpdateEnterprise([FromBody] UpdateEnterpriseCommand command)
        {
            try
            {
                if (command.Id == Guid.Empty)
                {
                    return BadRequest(new { message = "Id là bắt buộc." });
                }
                var result = await _mediator.Send(command);
                return Ok(new { message = "Cập nhật thành công!" });
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
    }
}
