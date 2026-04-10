using ERMS.Application.Features.Training.Commands.ConfirmTrainingRequest;
using ERMS.Application.Features.Training.Commands.CreateTrainingRequest;
using ERMS.Application.Features.Training.Commands.DeleteTrainingRequest;
using ERMS.Application.Features.Training.Commands.UpdateTrainingRequest;
using ERMS.Application.Features.Training.Queries.GetAllTrainingRequests;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ERMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TrainingRequestController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TrainingRequestController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Roles = AppRoles.DepartmentHead)]

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTrainingRequestCommand command)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var trainingRequestId = await _mediator.Send(command);
                return Ok(new
                {
                    message = "Tạo yêu cầu đào tạo thành công",
                    trainingRequestId
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] GetAllTrainingRequestsQuery query)
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

        [Authorize(Roles = AppRoles.HRManager)]

        [HttpPut("reject")]
        public async Task<IActionResult> Reject(
    [FromBody] RejectTrainingRequestCommand command)
        {
            var result = await _mediator.Send(command);

            return Ok(new
            {
                message = "Đã từ chối yêu cầu đào tạo",
                success = result
            });
        }

        [HttpPut("update")]
        public async Task<IActionResult> Update(UpdateTrainingRequestCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(new
            {
                message = "Cập nhật yêu cầu đào tạo thành công",
                success = result
            });
        }

        [Authorize(Roles = AppRoles.DepartmentHead)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var result = await _mediator.Send(new DeleteTrainingRequestCommand
                {
                    Id = id
                });

                if (!result)
                {
                    return NotFound(new { message = "Yêu cầu đào tạo không tìm thấy hoặc đã được xóa" });
                }

                return Ok(new
                {
                    message = "Xóa yêu cầu đào tạo thành công",
                    success = true
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
