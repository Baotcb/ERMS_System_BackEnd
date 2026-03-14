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
        private readonly ILogger<PublishCourseCommandHandler> _logger;
        private readonly IZoomService _zoomService;
        private readonly IEmailService _emailService;

        public PublishCourseCommandHandler(
            IERMSDbContext context,
            ICurrentUserService currentUserService,
            ILogger<PublishCourseCommandHandler> logger,
            IZoomService zoomService,
            IEmailService emailService)
        {
            _context = context;
            _currentUserService = currentUserService;
            _zoomService = zoomService;
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
                .FirstOrDefaultAsync(c =>
                    c.Id == request.Id &&
                    c.EnterpriseId == enterpriseId &&
                    !c.IsDeleted,
                    cancellationToken);

            if (course == null)
                throw new Exception("Không tìm thấy khóa học.");

            if (course.Status == "Published")
                throw new Exception("Khóa học đã được xuất bản.");

            if (string.IsNullOrEmpty(course.TrainerEmail))
                throw new Exception("Khóa học chưa có Trainer Email.");

            var trainerEmail = course.TrainerEmail;

            var isInternalTrainer = await _context.Employees
                .AnyAsync(e =>
                    e.User.Email == trainerEmail &&
                    e.EnterpriseId == enterpriseId &&
                    !e.IsDeleted,
                    cancellationToken);

            string? zoomLink = null;

            if (isInternalTrainer && request.TrainingType == "Online")
            {
                var meeting = await _zoomService.CreateMeetingAsync(
                    new ZoomMeetingRequest
                    {
                        Topic = course.CourseName,
                        Agenda = course.Description,
                        StartTime = request.StartTime,
                        Duration = course.DurationMinutes ?? 60,
                        Timezone = "UTC"
                    },
                    cancellationToken);

                zoomLink = meeting.JoinUrl;
            }

            string subject;
            string body;

            if (request.TrainingType == "Online")
            {
                subject = "Thông tin giảng dạy khóa học (Online)";
                body = CreateOnlineTrainingTemplate(
                    course.CourseName,
                    request.StartTime,
                    zoomLink ?? "Zoom link sẽ được cập nhật sau.");
            }
            else
            {
                subject = "Thông tin giảng dạy khóa học";
                body = CreateOfflineTrainingTemplate(
                    course.CourseName,
                    request.StartTime,
                    request.Location);
            }

            await _emailService.SendEmailAsync(trainerEmail, subject, body);

            course.Status = "Published";
            course.PublishedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Khóa học {CourseId} đã được xuất bản bởi người dùng {UserId}",
                course.Id,
                userId);

            return true;
        }

        private static string CreateOfflineTrainingTemplate(
    string courseName,
    DateTime startTime,
    string? location)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
<meta charset='UTF-8'>
<style>

body {{
    font-family: Arial;
    background:#f6f8ff;
    padding:40px;
}}

.container {{
    max-width:600px;
    margin:auto;
    background:white;
    border-radius:12px;
    overflow:hidden;
}}

.header {{
    background:#4f46e5;
    color:white;
    padding:20px;
    font-size:20px;
    font-weight:bold;
}}

.content {{
    padding:30px;
}}

.info {{
    background:#f9fafb;
    padding:15px;
    border-radius:8px;
    margin:20px 0;
}}

.footer {{
    text-align:center;
    font-size:12px;
    padding:20px;
    color:#888;
}}

</style>
</head>

<body>

<div class='container'>

<div class='header'>
ERMS Training Notification
</div>

<div class='content'>

<p>Xin chào Giảng viên,</p>

<p>Bạn được mời giảng dạy khóa học sau:</p>

<div class='info'>
<p><strong>Khóa học:</strong> {courseName}</p>
<p><strong>Hình thức:</strong> Offline</p>
<p><strong>Thời gian:</strong> {startTime:dd/MM/yyyy HH:mm}</p>
<p><strong>Địa điểm:</strong> {location}</p>
</div>

<p>Vui lòng chuẩn bị nội dung trước khi buổi đào tạo bắt đầu.</p>

<p>Trân trọng,<br><strong>ERMS Team</strong></p>

</div>

<div class='footer'>
© 2026 ERMS System
</div>

</div>

</body>
</html>";
        }

        private static string CreateOnlineTrainingTemplate(
    string courseName,
    DateTime startTime,
    string zoomLink)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
<meta charset='UTF-8'>
<style>

body {{
    font-family: Arial;
    background:#f6f8ff;
    padding:40px;
}}

.container {{
    max-width:600px;
    margin:auto;
    background:white;
    border-radius:12px;
    overflow:hidden;
}}

.header {{
    background:#2563eb;
    color:white;
    padding:20px;
    font-size:20px;
    font-weight:bold;
}}

.content {{
    padding:30px;
}}

.button {{display:inline-block;
    padding:14px 28px;
    background:#000000 !important;
    color:#ffffff !important;
    -webkit-text-fill-color:#ffffff !important;
    text-decoration:none !important;
    border-radius:8px;
    margin-top:15px;
    font-weight:700;
    border:2px solid #ffffff;
}}

.footer {{
    text-align:center;
    font-size:12px;
    padding:20px;
    color:#888;
}}

</style>
</head>

<body>

<div class='container'>

<div class='header'>
ERMS Online Training
</div>

<div class='content'>

<p>Xin chào Giảng viên,</p>

<p>Bạn được mời giảng dạy khóa học trực tuyến:</p>

<p><strong>Khóa học:</strong> {courseName}</p>
<p><strong>Thời gian:</strong> {startTime:dd/MM/yyyy HH:mm}</p>

<p>Nhấn vào nút bên dưới để tham gia Zoom:</p>

<a href='{zoomLink}' class='button' 
style=""background:#000000;color:#ffffff;-webkit-text-fill-color:#ffffff;
padding:14px 28px;text-decoration:none;border-radius:8px;font-weight:700;
display:inline-block;border:2px solid #ffffff;"">
Tham gia Zoom
</a>

<p>Hoặc dùng link sau:</p>

<p>{zoomLink}</p>

<p>Trân trọng,<br><strong>ERMS Team</strong></p>

</div>

<div class='footer'>
© 2026 ERMS System
</div>

</div>

</body>
</html>";
        }
    }
}
