using ERMS.Application.Interface;
using ERMS.Domain.Entities.Training;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Enrollments.Commands.AssignEmployeesToCourse
{
    public sealed class AssignEmployeesToCourseHandler
        : IRequestHandler<AssignEmployeesToCourseCommand, AssignEmployeesToCourseResult>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEmailService _emailService;
        private readonly ILogger<AssignEmployeesToCourseHandler> _logger;

        public AssignEmployeesToCourseHandler(
            IERMSDbContext context,
            ICurrentUserService currentUserService,
            IEmailService emailService,
            ILogger<AssignEmployeesToCourseHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<AssignEmployeesToCourseResult> Handle(
            AssignEmployeesToCourseCommand request,
            CancellationToken cancellationToken)
        {
            var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();

            if (enterpriseId == null)
                throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào.");

            var course = await _context.Courses
                .FirstOrDefaultAsync(c =>
                    c.Id == request.CourseId &&
                    c.EnterpriseId == enterpriseId.Value &&
                    !c.IsDeleted,
                    cancellationToken);

            if (course == null)
                throw new KeyNotFoundException("Không tìm thấy khóa học.");

            var existingEnrollments = await _context.Enrollments
                .Where(e => e.CourseId == request.CourseId && !e.IsDeleted)
                .Select(e => e.EmployeeId)
                .ToListAsync(cancellationToken);

            var employees = await _context.Employees
                .Where(e => request.EmployeeIds.Contains(e.Id))
                .Select(e => new
                {
                    e.Id,
                    e.User.Email,
                    e.User.FullName
                })
                .ToListAsync(cancellationToken);

            var assigned = new List<Guid>();
            var skipped = new List<Guid>();

            foreach (var employee in employees)
            {
                if (existingEnrollments.Contains(employee.Id))
                {
                    skipped.Add(employee.Id);
                    continue;
                }

                var enrollment = new Enrollment
                {
                    Id = Guid.NewGuid(),
                    CourseId = request.CourseId,
                    EmployeeId = employee.Id,
                    EnrolledAt = DateTime.UtcNow,
                    Status = "NotStarted",
                    Progress = 0
                };

                _context.Enrollments.Add(enrollment);
                assigned.Add(employee.Id);

                try
                {
                    var subject = $"Thông báo tham gia khóa học: {course.CourseName}";

                    var body = CreateCourseEnrollmentTemplate(
                        employee.FullName,
                        course.CourseName,
                        request.MeetUrl,
                        course.CreatedAt);

                    await _emailService.SendEmailAsync(employee.Email, subject, body);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Không thể gửi email cho {Email}", employee.Email);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            return new AssignEmployeesToCourseResult
            {
                TotalAssigned = assigned.Count,
                AssignedEmployeeIds = assigned,
                SkippedEmployeeIds = skipped
            };
        }

        private static string CreateCourseEnrollmentTemplate(
            string fullName,
            string courseTitle,
            string meetLink,
            DateTime startDate)
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
.button {{
    display:inline-block;
    padding:12px 24px;
    background:#4f46e5;
    color:white !important;
    text-decoration:none;
    border-radius:8px;
}}
</style>
</head>

<body>
<div class='container'>

<h2>Bạn đã được đăng ký vào khóa học</h2>

<p>Xin chào <b>{fullName}</b>,</p>

<p>Bạn vừa được thêm vào khóa học trong hệ thống <b>ERMS</b>.</p>

<p><b>Khóa học:</b> {courseTitle}</p>
<p><b>Thời gian bắt đầu:</b> {startDate:dd/MM/yyyy HH:mm}</p>

<p>Vui lòng tham gia lớp học qua Google Meet:</p>

<p>
<a class='button' href='{meetLink}'>Tham gia lớp học</a>
</p>

<p>Nếu bạn có thắc mắc, vui lòng liên hệ quản trị viên.</p>

<br>

<p>Trân trọng,<br>
<b>ERMS Training System</b></p>

</div>
</body>
</html>";
        }
    }
}