using ERMS.Application.Features.Training.Commands.CreateTrainingRequest;
using ERMS.Application.Features.Training.Queries.GetAllTrainingRequests;
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
    }
}
