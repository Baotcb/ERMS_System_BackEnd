using ERMS.Application.Features.Courses.Commands.CreateCourse;
using ERMS.Application.Features.Courses.Commands.PublishCourse;
using ERMS.Application.Features.Courses.Commands.UpdateCourse;
using ERMS.Application.Features.Courses.Queries.GetAllCourses;
using ERMS.Application.Features.Courses.Queries.GetCourseDetails;
using ERMS.Application.Features.Courses.Queries.GetCourseProgress;
using ERMS.Application.Features.CourseSkills.Commands.CreateCourseSkill;
using ERMS.Application.Features.Enrollments.Commands.AssignEmployeesToCourse;
using ERMS.Application.Features.Quizzes.Commands.CreateQuiz;
using ERMS.Application.Features.Quizzes.Commands.StartQuiz;
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
            try
            {
                var courseId =
                    await _mediator.Send(command);

                return Ok(new
                {
                    Message = "Course created successfully",
                    CourseId = courseId
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

        [HttpGet("department-training-results")]
        public async Task<IActionResult> GetDepartmentTrainingResults()
        {
            var result = await _mediator.Send(new Application.Features.Enrollments.Queries.GetDepartmentTrainingResults.GetDepartmentTrainingResultsQuery());
            return Ok(result);
        }

        [HttpPost("{courseId}/assign-employees")]
        public async Task<IActionResult> AssignEmployees(
    Guid courseId,
    
    [FromBody] AssignEmployeesRequest request)
        {
            var command = new AssignEmployeesToCourseCommand
            {
                CourseId = courseId,
                MeetUrl = request.MeetUrl,
                EmployeeIds = request.EmployeeIds
            };

            var result = await _mediator.Send(command);

            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, UpdateCourseCommand command)
        {
            if (id != command.Id)
                return BadRequest();

            try
            {
                var result = await _mediator.Send(command);
                return Ok(result);
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

        [HttpPost("{id}/publish")]
        public async Task<IActionResult> Publish(PublishCourseCommand request)
        {
            var result = await _mediator.Send(new PublishCourseCommand
            {
                Id = request.Id,
                Location = request.Location,
                StartTime = request.StartTime,
                TrainingType = request.TrainingType

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

        [HttpPost("{courseId}/quizzes/start")]
        public async Task<IActionResult> StartCourseQuiz(Guid courseId)
        {
            var result = await _mediator.Send(new StartQuizCommand
            {
                CourseId = courseId
            });

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

        [HttpPost("{courseId}/workshop-confirmation")]
        public async Task<IActionResult> ConfirmWorkshop(
            Guid courseId,
            [FromBody] Application.Features.Workshop.Commands.ConfirmWorkshop.ConfirmWorkshopCommand command)
        {
            command.CourseId = courseId;
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

        [HttpGet("{courseId}/workshop-confirmation")]
        public async Task<IActionResult> GetWorkshopConfirmation(Guid courseId)
        {
            var result = await _mediator.Send(
                new Application.Features.Workshop.Queries.GetWorkshopConfirmation.GetWorkshopConfirmationQuery
                {
                    CourseId = courseId
                });

            if (result == null) return NotFound();
            return Ok(result);
        }
    }
}