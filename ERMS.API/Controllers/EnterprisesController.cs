using ERMS.Application.Features.Enterprises.Commands.CreateEnterprise;
using ERMS.Application.Features.Enterprises.Commands.DeleteEnterprise;
using ERMS.Application.Features.Enterprises.Commands.UpdateEnterprise;
using ERMS.Application.Features.Enterprises.Queries.GetAllEnterprises;
using ERMS.Application.Features.Enterprises.Queries.GetEnterpriseById;
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
        /// Tạo mới doanh nghiệp
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateEnterpriseCommand command)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var enterpriseId = await _mediator.Send(command);

                return Ok(new
                {
                    message = "Tạo doanh nghiệp thành công",
                    enterpriseId
                });
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

        /// <summary>
        /// Lấy chi tiết doanh nghiệp theo Id
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (id == Guid.Empty)
                return BadRequest(new { message = "Id không hợp lệ" });

            try
            {
                var enterprise = await _mediator.Send(
                    new GetEnterpriseByIdQuery { Id = id });

                return Ok(enterprise);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách doanh nghiệp (phân trang)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] GetAllEnterprisesQuery query)
        {
            try
            {
                var result = await _mediator.Send(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteEnterprise([FromBody] DeleteEnterpriseCommand command)
        {
            try
            {
                if (command.Id == Guid.Empty)
                {
                    return BadRequest(new { message = "Id là bắt buộc." });
                }
                var result = await _mediator.Send(command);
                return Ok(new { message = "Xóa thành công!" });
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
