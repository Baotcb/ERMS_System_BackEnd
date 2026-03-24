using ERMS.Application.Features.Lessons.Commands.CreateLesson;
using ERMS.Application.Features.Lessons.Commands.UpdateLessonProgress;
using ERMS.Application.Features.Lessons.Queries.GetLessonProgress;
using ERMS.Application.Features.Lessons.Queries.GetLessonsByCourse;
using ERMS.Application.Interface;
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
        private readonly ICloudinaryService _cloudinaryService;

        public LessonsController(IMediator mediator,
            ICloudinaryService cloudinaryService)
        {
            _mediator = mediator;
            _cloudinaryService = cloudinaryService;
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

        [HttpGet("lesson-progress/course/{courseId}")]
        public async Task<IActionResult> GetLessonProgressByCourse(Guid courseId)
        {
            var result = await _mediator.Send(new GetLessonProgressByCourseQuery
            {
                CourseId = courseId
            });

            return Ok(result);
        }

        [HttpPost("upload-video")]
        [RequestSizeLimit(500_000_000)]
        public async Task<IActionResult> UploadVideo(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is required");

            using var stream = file.OpenReadStream();

            var result = await _cloudinaryService.UploadVideoAsync(stream, file.FileName);

            return Ok(new
            {
                VideoUrl = result.Url,
                DurationMinutes = result.DurationMinutes
            });
        }
    }
}
