using ERMS.Application.Features.Lessons.Commands.CreateLesson;
using ERMS.Application.Features.Lessons.Commands.UpdateLessonProgress;
using ERMS.Application.Features.Lessons.Queries.GetLessonProgress;
using ERMS.Application.Features.Lessons.Queries.GetLessonsByCourse;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ERMS.API.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LessonsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public LessonsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateLessonCommand command)
        {
            var id = await _mediator.Send(command);
            return Ok(id);
        }

        [HttpGet("course/{courseId}")]
        public async Task<IActionResult> GetByCourse(Guid courseId)
        {
            var result = await _mediator.Send(new GetLessonsByCourseQuery
            {
                CourseId = courseId
            });

            return Ok(result);
        }

        [HttpPost("lesson-progress")]
        public async Task<IActionResult> UpdateLessonProgress(UpdateLessonProgressCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpGet("lesson-progress/{enrollmentId}")]
        public async Task<IActionResult> GetLessonProgress(Guid enrollmentId)
        {
            var result = await _mediator.Send(new GetLessonProgressByEnrollmentQuery
            {
                EnrollmentId = enrollmentId
            });

            return Ok(result);
        }
    }
}
