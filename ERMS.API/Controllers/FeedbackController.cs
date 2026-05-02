using ERMS.Application.Features.Feedback.Commands.DeleteReply;
using ERMS.Application.Features.Feedback.Commands.ReplyFeedback;
using ERMS.Application.Features.Feedback.Commands.SubmitCourseFeedback;
using ERMS.Application.Features.Feedback.Commands.UpdateReply;
using ERMS.Application.Features.Feedback.Queries.GetCourseFeedbacks;
using ERMS.Application.Features.Feedback.Queries.GetFeedbackReplies;
using ERMS.Application.Features.Feedback.Queries.GetTrainerFeedbacks;
using ERMS.Application.Interface;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FeedbackController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public FeedbackController(IMediator mediator, IERMSDbContext context, ICurrentUserService currentUserService)
        {
            _mediator = mediator;
            _context = context;
            _currentUserService = currentUserService;
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
         
        [HttpGet("check/{courseId}")]
        public async Task<IActionResult> CheckFeedback(Guid courseId)
        {
            var userId = _currentUserService.UserId;
            if (userId == null) return Ok(new { hasSubmitted = false });

            var employee = await _context.Employees
                .FirstOrDefaultAsync(x => x.UserId == userId);
            if (employee == null) return Ok(new { hasSubmitted = false });

            var feedback = await _context.CourseFeedbacks
                .Where(x => x.CourseId == courseId &&
                            x.EmployeeId == employee.Id &&
                            !x.IsDeleted)
                .Select(x => new
                {
                    feedbackId = x.Id,
                    courseRating = x.CourseRating,
                    trainerRating = x.TrainerRating,
                    comment = x.Comment,
                    isAnonymous = x.IsAnonymous,
                    createdAt = x.CreatedAt
                })
                .FirstOrDefaultAsync();

            return Ok(new
            {
                hasSubmitted = feedback != null,
                feedbackData = feedback
            });
        }

        [HttpPost("{feedbackId}/replies")]
        public async Task<IActionResult> ReplyFeedback(
    int feedbackId,
    [FromBody] ReplyFeedbackCommand command)
        {
            try
            {
                command.FeedbackId = feedbackId;

                var result = await _mediator.Send(command);

                return Ok(new
                {
                    replyId = result,
                    message = "Phản hồi thành công"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{feedbackId}/replies")]
        public async Task<IActionResult> GetReplies(int feedbackId)
        {
            var result = await _mediator.Send(new GetFeedbackRepliesQuery
            {
                FeedbackId = feedbackId
            });

            return Ok(result);
        }

        [HttpPut("replies/{replyId}")]
        public async Task<IActionResult> UpdateReply(
    int replyId,
    [FromBody] UpdateReplyCommand command)
        {
            try
            {
                command.ReplyId = replyId;

                await _mediator.Send(command);

                return Ok(new { message = "Updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("replies/{replyId}")]
        public async Task<IActionResult> DeleteReply(int replyId)
        {
            try
            {
                await _mediator.Send(new DeleteReplyCommand
                {
                    ReplyId = replyId
                });

                return Ok(new { message = "Deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
