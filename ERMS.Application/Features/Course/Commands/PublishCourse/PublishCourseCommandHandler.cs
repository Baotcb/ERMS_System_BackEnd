using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.Courses.Commands.PublishCourse
{
    public sealed class PublishCourseCommandHandler
        : IRequestHandler<PublishCourseCommand, bool>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEmailService _emailService;
        private readonly ILogger<PublishCourseCommandHandler> _logger;

        public PublishCourseCommandHandler(
            IERMSDbContext context,
            ICurrentUserService currentUserService,
            IEmailService emailService,
            ILogger<PublishCourseCommandHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<bool> Handle(
            PublishCourseCommand request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (userId == null)
                throw new UnauthorizedAccessException("Người dùng chưa đăng nhập.");

            var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();

            if (enterpriseId == null)
                throw new Exception("Người dùng không thuộc doanh nghiệp nào.");

            var course = await _context.Courses
                .Include(c => c.Trainer)
                    .ThenInclude(t => t.User)
                .FirstOrDefaultAsync(c =>
                    c.Id == request.Id &&
                    c.EnterpriseId == enterpriseId &&
                    !c.IsDeleted,
                    cancellationToken);

            if (course == null)
                throw new Exception("Không tìm thấy khóa học.");

            if (course.Status == "Published")
                throw new Exception("Khóa học đã được xuất bản.");

            // Kiểm tra khóa học có bài học hay chưa
            var lessonCount = await _context.Lessons
                .CountAsync(l =>
                    l.CourseId == course.Id &&
                    !l.IsDeleted,
                    cancellationToken);

            if (lessonCount == 0)
                throw new Exception("Không thể xuất bản khóa học khi chưa có bài học.");

            course.Status = "Published";
            course.PublishedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Gửi email cho Trainer
            try
            {
                var trainer = course.Trainer;

                if (trainer?.User?.Email != null)
                {
                    var subject = $"Khóa học {course.CourseName} đã được xuất bản";

                    var body = CreatePublishNotificationTemplate(
                        trainer.User.FullName,
                        course.CourseName,
                        course.CourseCode);

                    await _emailService.SendEmailAsync(
                        trainer.User.Email,
                        subject,
                        body);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Không thể gửi email publish course {CourseId}",
                    course.Id);
            }

            _logger.LogInformation(
                "Khóa học {CourseId} đã được xuất bản bởi người dùng {UserId}",
                course.Id,
                userId);

            return true;
        }

        private static string CreatePublishNotificationTemplate(
            string trainerName,
            string courseName,
            string courseCode)
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

<h2>Khóa học đã được xuất bản</h2>

<p>Xin chào <b>{trainerName}</b>,</p>

<p>Khóa học bạn phụ trách đã được <b>xuất bản</b> trên hệ thống ERMS.</p>

<p><b>Tên khóa học:</b> {courseName}</p>
<p><b>Mã khóa học:</b> {courseCode}</p>

<p>Bạn có thể bắt đầu chuẩn bị nội dung và lịch giảng dạy.</p>

<br>

<p>Trân trọng,<br>
<b>ERMS Training System</b></p>

</div>
</body>
</html>";
        }
    }
}