using ERMS.Application.Features.Feedback.Commands.SubmitCourseFeedback;
using ERMS.Application.Features.Feedback.Queries.GetCourseFeedbacks;
using ERMS.Application.Features.Feedback.Queries.GetTrainerFeedbacks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FeedbackController : ControllerBase
    {
        private readonly IMediator _mediator;

        public FeedbackController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> SubmitFeedback([FromBody] SubmitCourseFeedbackCommand command)
        {
            try
            {
                var result = await _mediator.Send(command);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllFeedbacks()
        {
            var result = await _mediator.Send(new GetCourseFeedbacksQuery());
            return Ok(result);
        }

        [HttpGet("trainer")]
        public async Task<IActionResult> GetTrainerFeedbacks()
        {
            var result = await _mediator.Send(new GetTrainerFeedbacksQuery());
            return Ok(result);
        }
    }
}
