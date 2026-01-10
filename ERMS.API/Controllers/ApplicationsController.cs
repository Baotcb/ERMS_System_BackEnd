using ERMS.Application.Features.Applications.Commands.CreateApplication;
using ERMS.Application.Features.Applications.Commands.DeleteApplication;
using ERMS.Application.Features.Applications.Commands.UpdateApplication;
using ERMS.Application.Features.Applications.Queries.GetAllApplications;
using ERMS.Application.Features.Applications.Queries.GetApplicationById;
using ERMS.Domain.Constants.Roles;
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
    public class ApplicationsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ApplicationsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> CreateApplication([FromBody] CreateApplicationCommand command)
        {
            try
            {
                var applicationId = await _mediator.Send(command);
                return Ok(new { message = "Ứng tuyển thành công!", applicationId });
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
        public async Task<IActionResult> UpdateApplication([FromBody] UpdateApplicationCommand command)
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

        [HttpDelete]
        public async Task<IActionResult> DeleteApplication([FromBody] DeleteApplicationCommand command)
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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetApplicationById(Guid id)
        {
            try
            {
                var application = await _mediator.Send(new GetApplicationByIdQuery { Id = id });
                return Ok(application);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllApplications([FromQuery] GetAllApplicationsQuery query)
        {
            try
            {
                var applications = await _mediator.Send(query);
                return Ok(applications);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}