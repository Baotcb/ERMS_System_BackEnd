using ERMS.Application.Features.Courses.Commands.CreateCourse;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CourseController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CourseController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> CreateCourse(
            CreateCourseCommand command)
        {
            var courseId =
                await _mediator.Send(command);

            return Ok(new
            {
                Message = "Course created successfully",
                CourseId = courseId
            });
        }
    }
}