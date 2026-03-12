using ERMS.Application.Interface;
using ERMS.Domain.Entities.Training;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.Courses.Commands.CreateCourse
{
    public sealed class CreateCourseCommandHandler
        : IRequestHandler<CreateCourseCommand, Guid>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEmailService _emailService;
        private readonly ILogger<CreateCourseCommandHandler> _logger;

        public CreateCourseCommandHandler(
            IERMSDbContext context,
            ICurrentUserService currentUserService,
            IEmailService emailService,
            ILogger<CreateCourseCommandHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<Guid> Handle(
            CreateCourseCommand request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (userId == null)
                throw new UnauthorizedAccessException("Người dùng chưa được xác thực");

            var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();

            if (enterpriseId == null)
                throw new Exception("Người dùng không thuộc doanh nghiệp nào");

            // Check duplicate CourseCode
            var existedCode = await _context.Courses
                .AnyAsync(c =>
                    c.CourseCode == request.CourseCode &&
                    c.EnterpriseId == enterpriseId &&
                    !c.IsDeleted,
                    cancellationToken);

            if (existedCode)
                throw new Exception("Mã khóa học đã tồn tại");

            // Lấy Trainer + User
            var trainer = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e =>
                    e.Id == request.TrainerId &&
                    e.EnterpriseId == enterpriseId &&
                    !e.IsDeleted,
                    cancellationToken);

            if (trainer == null)
                throw new Exception("Không tìm thấy giảng viên");

            // Auto-promote employee to trainer when assigned to a course.
            if (!trainer.IsTrainer)
            {
                trainer.IsTrainer = true;
                trainer.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "Employee {EmployeeId} auto-promoted to trainer while creating course {CourseCode}",
                    trainer.Id,
                    request.CourseCode);
            }

            // Create Course
            var course = new Course
            {
                Id = Guid.CreateVersion7(),
                EnterpriseId = enterpriseId.Value,
                TrainingPlanId = request.TrainingPlanId,
                CourseName = request.CourseName,
                CourseCode = request.CourseCode,
                Description = request.Description,
                ThumbnailUrl = request.ThumbnailUrl,
                TrainerId = trainer.Id,
                DurationMinutes = request.DurationMinutes,
                Level = request.Level,
                IsMandatory = request.IsMandatory,
                MaxEnrollments = request.MaxEnrollments,
                EnrollmentDeadline = request.EnrollmentDeadline,
                CompletionCriteria = request.CompletionCriteria,
                Status = "Draft",
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _context.Courses.Add(course);

            await _context.SaveChangesAsync(cancellationToken);

            // Gửi mail cho Trainer
            try
            {
                if (!string.IsNullOrWhiteSpace(trainer.User?.Email))
                {
                    var subject = $"Bạn được phân công giảng dạy khóa học {course.CourseName}";

                    var body = CreateTrainerAssignTemplate(
                        trainer.User.FullName,
                        course.CourseName,
                        course.CourseCode,
                        course.DurationMinutes ?? 0);

                    await _emailService.SendEmailAsync(
                        trainer.User.Email,
                        subject,
                        body);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Không thể gửi email thông báo trainer cho khóa học {CourseId}",
                    course.Id);
            }

            _logger.LogInformation(
                "Course {CourseCode} created by User {UserId} in Enterprise {EnterpriseId}",
                course.CourseCode,
                userId,
                enterpriseId);

            return course.Id;
        }

        private static string CreateTrainerAssignTemplate(
            string trainerName,
            string courseName,
            string courseCode,
            int durationMinutes)
        {
            return $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
<meta charset='utf-8'>
<style>
body {{
    font-family: Arial, sans-serif;
    background:#f6f8ff;
}}
.container {{
    max-width:560px;
    margin:auto;
    background:white;
    border-radius:12px;
    padding:30px;
}}
</style>
</head>

<body>
<div class='container'>

<h2>Bạn được phân công làm giảng viên</h2>

<p>Xin chào <b>{trainerName}</b>,</p>

<p>Bạn vừa được phân công giảng dạy khóa học trên hệ thống <b>ERMS</b>.</p>

<p><b>Tên khóa học:</b> {courseName}</p>
<p><b>Mã khóa học:</b> {courseCode}</p>
<p><b>Thời lượng:</b> {durationMinutes} phút</p>

<p>Vui lòng chuẩn bị nội dung giảng dạy cho khóa học.</p>

<br>

<p>Trân trọng,<br>
<b>ERMS Training System</b></p>

</div>
</body>
</html>";
        }
    }
}