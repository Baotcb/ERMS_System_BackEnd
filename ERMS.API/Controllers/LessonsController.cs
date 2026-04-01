using ERMS.Application.Features.Lessons.Commands.CreateLesson;
using ERMS.Application.Features.Lessons.Commands.UpdateLesson;
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
        private readonly IERMSDbContext _context;

        public LessonsController(IMediator mediator,
            ICloudinaryService cloudinaryService,
            IERMSDbContext context)
        {
            _mediator = mediator;
            _cloudinaryService = cloudinaryService;
            _context = context;
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

        /// <summary>
        /// Upload tài liệu đính kèm cho bài học (lưu vào Cloudinary → cập nhật DocumentUrl)
        /// </summary>
        [HttpPost("{lessonId}/material")]
        [RequestSizeLimit(50_000_000)]
        public async Task<IActionResult> UploadMaterial(Guid lessonId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "File is required" });

            var lesson = await _context.Lessons.FindAsync(lessonId);
            if (lesson == null)
                return NotFound(new { message = "Không tìm thấy bài học." });

            // Upload to Cloudinary as raw file
            using var stream = file.OpenReadStream();
            var uploadResult = await _cloudinaryService.UploadPdfAsync(stream, file.FileName);

            lesson.DocumentUrl = uploadResult.Url;
            lesson.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = $"material-{lesson.Id}",
                lessonId = lesson.Id.ToString(),
                title = file.FileName,
                fileUrl = uploadResult.Url,
                fileType = file.ContentType ?? "FILE",
                fileSize = file.Length
            });
        }

        /// <summary>
        /// Cập nhật DocumentUrl trực tiếp (khi FE đã upload qua Cloudinary rồi)
        /// </summary>
        [HttpPut("{lessonId}/document-url")]
        public async Task<IActionResult> UpdateDocumentUrl(Guid lessonId, [FromBody] UpdateDocumentUrlRequest request)
        {
            var lesson = await _context.Lessons.FindAsync(lessonId);
            if (lesson == null)
                return NotFound(new { message = "Không tìm thấy bài học." });

            lesson.DocumentUrl = request.DocumentUrl;
            lesson.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã cập nhật tài liệu thành công." });
        }

        [HttpPut("{lessonId}")]
        public async Task<IActionResult> Update(Guid lessonId, UpdateLessonCommand command)
        {
            
            command.Id = lessonId;

            var result = await _mediator.Send(command);
            return Ok(result);
        }

        public class UpdateDocumentUrlRequest
        {
            public string? DocumentUrl { get; set; }
        }
    }
}
