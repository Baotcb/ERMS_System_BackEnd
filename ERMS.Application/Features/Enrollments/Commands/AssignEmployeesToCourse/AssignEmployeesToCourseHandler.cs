using ERMS.Application.Interface;
using ERMS.Domain.Entities.Training;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace ERMS.Application.Features.Enrollments.Commands.AssignEmployeesToCourse
{
    public sealed class AssignEmployeesToCourseHandler
        : IRequestHandler<AssignEmployeesToCourseCommand, AssignEmployeesToCourseResult>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IZoomService _zoomService;
        private readonly IEmailService _emailService;

        public AssignEmployeesToCourseHandler(
            IERMSDbContext context,
            ICurrentUserService currentUserService,
            IZoomService zoomService,
            IEmailService emailService)
        {
            _context = context;
            _currentUserService = currentUserService;
            _zoomService = zoomService;
            _emailService = emailService;
        }

        public async Task<AssignEmployeesToCourseResult> Handle(
            AssignEmployeesToCourseCommand request,
            CancellationToken cancellationToken)
        {
            // 1. Kiểm tra quyền hạn và sự tồn tại của Course
            var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();
            if (enterpriseId == null) throw new UnauthorizedAccessException("Không xác định được doanh nghiệp.");

            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == request.CourseId && c.EnterpriseId == enterpriseId && !c.IsDeleted, cancellationToken);

            if (course == null) throw new KeyNotFoundException("Khóa học không tồn tại hoặc đã bị xóa.");

            // 2. Xử lý ghi danh (Enrollment) - Tránh trùng lặp
            var existingEmployeeIds = await _context.Enrollments
                .Where(e => e.CourseId == request.CourseId)
                .Select(e => e.EmployeeId)
                .ToListAsync(cancellationToken);

            var newEmployeeIds = request.EmployeeIds.Distinct().Except(existingEmployeeIds).ToList();
            var skippedIds = request.EmployeeIds.Distinct().Intersect(existingEmployeeIds).ToList();

            foreach (var empId in newEmployeeIds)
            {
                _context.Enrollments.Add(new Enrollment
                {
                    Id = Guid.NewGuid(),
                    CourseId = request.CourseId,
                    EmployeeId = empId,
                    EnrolledAt = DateTime.UtcNow,
                    Status = "NotStarted"
                });
            }
            await _context.SaveChangesAsync(cancellationToken);

            // 3. Chuẩn bị hạ tầng Online (Zoom) nếu cần
            string? zoomLink = null;
            if (course.IsOnline)
            {
                // Chỉ tạo Zoom nếu Trainer là người trong hệ thống (hoặc theo logic riêng của bạn)
                    var isInternalTrainer = await _context.Employees.AnyAsync(e => e.User.Email == course.TrainerEmail, cancellationToken);
                    if (isInternalTrainer)
                    {
                        var meeting = await _zoomService.CreateMeetingAsync(new ZoomMeetingRequest
                        {
                            Topic = $"[ERMS] {course.CourseName}",
                            StartTime = course.StartTime,
                            Duration = course.DurationMinutes ?? 60
                        }, cancellationToken);
                        zoomLink = meeting.JoinUrl;
                    }
                }

            // 4. Gửi Email thông báo (Phân biệt Trainer & Trainee)
                await SendNotificationEmails(course, newEmployeeIds, zoomLink, cancellationToken);

            return new AssignEmployeesToCourseResult
            {
                TotalAssigned = newEmployeeIds.Count,
                AssignedEmployeeIds = newEmployeeIds,
                SkippedEmployeeIds = skippedIds
            };
        }

        private async Task SendNotificationEmails(Course course, List<Guid> assignedIds, string? zoomLink, CancellationToken ct)
        {
            // Lấy danh sách Email học viên
            var traineeEmails = await _context.Employees
                .Where(e => assignedIds.Contains(e.Id))
                .Select(e => e.User.Email)
                .ToListAsync(ct);

            // Gửi cho Trainer (Nội dung hướng dẫn giảng dạy)
            var trainerSubject = $"[GIẢNG VIÊN] Lịch đào tạo khóa: {course.CourseName}";
            var trainerBody = BuildHtmlEmail(course, zoomLink, isTrainer: true);
            await _emailService.SendEmailAsync(course.TrainerEmail, trainerSubject, trainerBody);

            // Gửi cho Trainees (Nội dung mời học)
            var traineeSubject = $"[HỌC VIÊN] Mời tham gia khóa học: {course.CourseName}";
            var traineeBody = BuildHtmlEmail(course, zoomLink, isTrainer: false);

            foreach (var email in traineeEmails)
            {
                await _emailService.SendEmailAsync(email, traineeSubject, traineeBody);
            }
        }

        private string BuildHtmlEmail(Course course, string? zoomLink, bool isTrainer)
        {
            var brandColor = isTrainer ? "#1a237e" : "#00695c"; // Trainer: Xanh đậm, Trainee: Xanh lá đậm
            var roleTitle = isTrainer ? "THÔNG TIN GIẢNG VIÊN" : "THÔNG TIN GHI DANH";
            var welcomeMsg = isTrainer
                ? "Chào thầy/cô, hệ thống đã sắp xếp lịch giảng dạy cho khóa học sau:"
                : "Chúc mừng bạn! Bạn đã được ghi danh thành công vào khóa học:";

            return $@"
<div style='background-color: #f4f7f6; padding: 20px; font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif;'>
    <div style='max-width: 600px; margin: auto; background: white; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.1);'>
        <div style='background: {brandColor}; color: white; padding: 30px; text-align: center;'>
            <h1 style='margin: 0; font-size: 24px;'>ERMS TRAINING</h1>
            <p style='margin: 10px 0 0; opacity: 0.9; font-weight: 300;'>{roleTitle}</p>
        </div>

        <div style='padding: 30px; color: #333;'>
            <p style='font-size: 16px;'>{welcomeMsg}</p>
            
            <div style='background: #f9f9f9; border-left: 4px solid {brandColor}; padding: 20px; margin: 25px 0;'>
                <h3 style='margin-top: 0; color: {brandColor};'>{course.CourseName}</h3>
                <table style='width: 100%; font-size: 14px; border-collapse: collapse;'>
                    <tr>
                        <td style='padding: 8px 0; color: #666; width: 100px;'>Thời gian:</td>
                        <td style='padding: 8px 0; font-weight: bold;'>{course.StartTime:dd/MM/yyyy HH:mm}</td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; color: #666;'>Hình thức:</td>
                        <td style='padding: 8px 0;'>{(course.IsOnline ? "Trực tuyến (Online)" : "Tại văn phòng (Offline)")}</td>
                    </tr>
                    {(course.IsOnline ? $@"
                    <tr>
                        <td style='padding: 8px 0; color: #666;'>Nền tảng:</td>
                        <td style='padding: 8px 0;'>Zoom Meeting</td>
                    </tr>" : $@"
                    <tr>
                        <td style='padding: 8px 0; color: #666;'>Địa điểm:</td>
                        <td style='padding: 8px 0; font-weight: bold;'>{course.Location}</td>
                    </tr>")}
                </table>
            </div>

            {(!string.IsNullOrEmpty(zoomLink) ? $@"
            <div style='text-align: center; margin: 30px 0;'>
                <a href='{zoomLink}' style='background: {brandColor}; color: white; padding: 14px 28px; text-decoration: none; border-radius: 8px; font-weight: bold; display: inline-block;'>
                    {(isTrainer ? "BẮT ĐẦU BUỔI HỌC" : "THAM GIA NGAY")}
                </a>
                <p style='font-size: 12px; color: #888; margin-top: 10px;'>Hoặc truy cập link: <a href='{zoomLink}' style='color: {brandColor};'>{zoomLink}</a></p>
            </div>" : "")}

            <hr style='border: 0; border-top: 1px solid #eee; margin: 30px 0;'>
            
            <p style='font-size: 13px; color: #777; line-height: 1.6;'>
                {(isTrainer ? "Vui lòng chuẩn bị giáo án và tài liệu trước 15 phút." : "Vui lòng có mặt đúng giờ để buổi học diễn ra tốt đẹp.")} <br>
                Đây là email tự động từ hệ thống quản lý đào tạo ERMS.
            </p>
        </div>

        <div style='background: #eee; padding: 15px; text-align: center; font-size: 12px; color: #999;'>
            © 2026 ERMS Enterprise System. All rights reserved.
        </div>
    </div>
</div>";
        }
    }
}