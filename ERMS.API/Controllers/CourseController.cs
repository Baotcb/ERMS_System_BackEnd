using ERMS.Application.Features.Courses.Commands.CreateCourse;
using ERMS.Application.Features.Courses.Commands.PublishCourse;
using ERMS.Application.Features.Courses.Commands.UpdateCourse;
using ERMS.Application.Features.Courses.Queries.GetAllCourses;
using ERMS.Application.Features.Courses.Queries.GetCourseDetails;
using ERMS.Application.Features.Courses.Queries.GetCourseProgress;
using ERMS.Application.Features.CourseSkills.Commands.CreateCourseSkill;
using ERMS.Application.Features.Enrollments.Commands.AssignEmployeesToCourse;
using ERMS.Application.Features.Quizzes.Commands.CreateQuiz;
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

        [HttpGet]
        public async Task<IActionResult> GetAllCourses(
            [FromQuery] GetAllCourseQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetCourseDetails(
            Guid id)
        {
            var query = new GetCourseDetailsQuery
            {
                Id = id
            };
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpPost("{courseId}/assign-employees")]
        public async Task<IActionResult> AssignEmployees(
    Guid courseId,
    [FromBody] List<Guid> employeeIds)
        {
            var command = new AssignEmployeesToCourseCommand
            {
                CourseId = courseId,
                EmployeeIds = employeeIds
            };

            var result = await _mediator.Send(command);

            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, UpdateCourseCommand command)
        {
            if (id != command.Id)
                return BadRequest();

            var result = await _mediator.Send(command);

            return Ok(result);
        }

        [HttpPost("{id}/publish")]
        public async Task<IActionResult> Publish(Guid id)
        {
            var result = await _mediator.Send(new PublishCourseCommand
            {
                Id = id
            });

            return Ok(result);
        }

        [HttpPost("{courseId}/quizzes")]
        public async Task<IActionResult> CreateQuiz(
        Guid courseId,
        [FromBody] CreateQuizCommand command)
        {
            command.CourseId = courseId;

            var result = await _mediator.Send(command);

            return Ok(result);
        }

        [HttpPost("course-skill")]
        public async Task<IActionResult> CreateCourseSkill(CreateCourseSkillCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpGet("{courseId}/progress")]
        public async Task<IActionResult> GetProgress(Guid courseId)
        {
            var result = await _mediator.Send(new GetCourseProgressQuery
            {
                CourseId = courseId
            });

            return Ok(result);
        }
    }
}