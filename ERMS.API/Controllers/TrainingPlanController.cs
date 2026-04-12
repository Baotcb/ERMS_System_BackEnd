using ERMS.Application.Features.Training.Commands.ApproveTrainingPlan;
using ERMS.Application.Features.Training.Commands.CloseTrainingPlan;
using ERMS.Application.Features.Training.Commands.CreateTrainingPlan;
using ERMS.Application.Features.Training.Commands.CreateTrainingRequest;
using ERMS.Application.Features.Training.Commands.DeleteTrainingPlan;
using ERMS.Application.Features.Training.Commands.RejectTrainingPlan;
using ERMS.Application.Features.Training.Commands.UpdateTrainingPlan;
using ERMS.Application.Features.Training.Queries.GetAllTrainingPlans;
using ERMS.Application.Features.Training.Queries.GetAllTrainingRequests;
using ERMS.Application.Features.Training.Queries.GetDepartmentTrainingSummary;
using ERMS.Application.Features.Training.Queries.GetTrainingPlanDetail;
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
    public class TrainingPlanController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TrainingPlanController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTrainingPlanCommand command)
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

        [Authorize(Roles = AppRoles.HRManager+","+AppRoles.Director+","+AppRoles.DepartmentHead )]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] GetAllTrainingPlansQuery query)
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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetail(Guid id)
        {
            try
            {
                var result = await _mediator.Send(
                    new GetTrainingPlanDetailQuery { Id = id });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
    Guid id,
    [FromBody] UpdateTrainingPlanCommand command)
        {
            if (id != command.Id)
                return BadRequest("Id không hợp lệ");

            try
            {
                var result = await _mediator.Send(command);

                return Ok(new
                {
                    message = "Cập nhật kế hoạch đào tạo thành công",
                    trainingPlanId = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = AppRoles.Director)]
        [HttpPut("approve")]
        public async Task<IActionResult> Approve(
            [FromBody] ApproveTrainingPlanCommand command)
        {
            var result = await _mediator.Send(command);

            return Ok(new
            {
                message = "Training plan approved successfully",
                success = result
            });
        }

        [Authorize(Roles = AppRoles.Director)]
        [HttpPut("reject")]
        public async Task<IActionResult> Reject(
    [FromBody] RejectTrainingPlanCommand command)
        {
            var result = await _mediator.Send(command);

            return Ok(new
            {
                message = "Training plan rejected successfully",
                success = result
            });
        }
        [Authorize(Roles = AppRoles.HRManager + "," + AppRoles.Director)]
        [HttpPut("close")]
        public async Task<IActionResult> Close(
    [FromBody] CloseTrainingPlanCommand command)
        {
            try
            {
                var result = await _mediator.Send(command);

                return Ok(new
                {
                    message = "Training plan closed successfully",
                    success = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var result = await _mediator.Send(new DeleteTrainingPlanCommand { Id = id });
                return Ok(new
                {
                    message = "Training plan deleted successfully",
                    success = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = AppRoles.DepartmentHead)]
        [HttpGet("department-training-summary")]
        public async Task<IActionResult> GetDepartmentTrainingSummary()
        {
            var result = await _mediator.Send(new GetDepartmentTrainingSummaryQuery());
            return Ok(result);
        }
    }
}
