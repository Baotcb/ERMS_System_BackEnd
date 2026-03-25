using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.Courses.Commands.UpdateCourse
{
    public sealed class UpdateCourseCommandHandler
        : IRequestHandler<UpdateCourseCommand, Guid>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<UpdateCourseCommandHandler> _logger;

        public UpdateCourseCommandHandler(
            IERMSDbContext context,
            ICurrentUserService currentUserService,
            ILogger<UpdateCourseCommandHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<Guid> Handle(
            UpdateCourseCommand request,
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

            // Kiểm tra trùng CourseCode
            var existedCode = await _context.Courses
                .AnyAsync(c =>
                    c.CourseCode == request.CourseCode &&
                    c.Id != request.Id &&
                    c.EnterpriseId == enterpriseId &&
                    !c.IsDeleted,
                    cancellationToken);

            if (existedCode)
                throw new Exception("Mã khóa học đã tồn tại.");

           

            // Cập nhật khóa học
            var oldTrainerEmail = course.TrainerEmail; // lưu trước khi ghi đè
            course.TrainingPlanId = request.TrainingPlanId;
            course.CourseName = request.CourseName;
            course.CourseCode = request.CourseCode;
            course.Description = request.Description;
            course.ThumbnailUrl = request.ThumbnailUrl;
            course.TrainerEmail = request.TrainerEmail;
            course.DurationMinutes = request.DurationMinutes;
            course.StartTime = request.StartTime;
            course.IsOnline = request.IsOnline;
            course.Location = request.Location;
            course.Level = request.Level;
            course.IsMandatory = request.IsMandatory;
            course.MaxEnrollments = request.MaxEnrollments;
            course.EnrollmentDeadline = request.EnrollmentDeadline;
            course.CompletionCriteria = request.CompletionCriteria;

            // ✅ Bảo toàn ContentManagerEmail: nếu TrainerEmail thay đổi → re-detect
            if (!string.Equals(oldTrainerEmail, request.TrainerEmail, StringComparison.OrdinalIgnoreCase))
            {
                var isInternalTrainer = await _context.Employees
                    .AnyAsync(e => e.User.Email == request.TrainerEmail, cancellationToken);
                course.ContentManagerEmail = isInternalTrainer
                    ? request.TrainerEmail
                    : _currentUserService.Email;
            }
            // Nếu ContentManagerEmail đang null (legacy data) → gán mặc định
            else if (string.IsNullOrEmpty(course.ContentManagerEmail))
            {
                course.ContentManagerEmail = course.TrainerEmail;
            }

            course.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Khóa học {CourseId} đã được cập nhật bởi người dùng {UserId}",
                course.Id,
                userId);

            return course.Id;
        }
    }
}