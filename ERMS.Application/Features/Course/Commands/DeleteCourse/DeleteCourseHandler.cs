using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.Courses.Commands.DeleteCourse
{
    public sealed class DeleteCourseHandler : IRequestHandler<DeleteCourseCommand, Guid>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<DeleteCourseHandler> _logger;

        public DeleteCourseHandler(
            IERMSDbContext context,
            ICurrentUserService currentUserService,
            ILogger<DeleteCourseHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<Guid> Handle(DeleteCourseCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId
                ?? throw new UnauthorizedAccessException("Người dùng chưa đăng nhập.");

            var enterpriseId = await _currentUserService.GetEnterpriseIdAsync()
                ?? throw new Exception("Người dùng không thuộc doanh nghiệp.");

            var roles = _currentUserService.Roles ?? new string[] { };
            var email = _currentUserService.Email;

            var course = await _context.Courses
                .Include(c => c.Enrollments)
                .Include(c => c.Lessons)
                .Include(c => c.CourseFeedbacks)
                .FirstOrDefaultAsync(c =>
                    c.Id == request.Id &&
                    c.EnterpriseId == enterpriseId &&
                    !c.IsDeleted,
                    cancellationToken);

            if (course == null)
                throw new KeyNotFoundException("Không tìm thấy khóa học.");

            //  Check quyền
            var isHR = roles.Contains(AppRoles.HRManager);
            var isContentManager = course.ContentManagerEmail == email;

            if (!isHR && !isContentManager)
                throw new UnauthorizedAccessException("Bạn không có quyền xóa khóa học.");

            //  Không cho xóa nếu đã publish
            if (course.PublishedAt.HasValue)
                throw new Exception("Không thể xóa khóa học đã được publish.");

            //  Có enrollment
            var hasEnrollment = await _context.Enrollments
                .AnyAsync(e => e.CourseId == course.Id && !e.IsDeleted, cancellationToken);

            if (hasEnrollment)
                throw new Exception("Không thể xóa khóa học đã có học viên.");

            //  Có lesson progress
            var hasProgress = await _context.LessonProgresses
                .AnyAsync(lp => lp.Lesson.CourseId == course.Id, cancellationToken);

            if (hasProgress)
                throw new Exception("Không thể xóa khóa học đã có tiến trình học.");

            //  Có feedback
            var hasFeedback = await _context.CourseFeedbacks
                .AnyAsync(f => f.CourseId == course.Id && !f.IsDeleted, cancellationToken);

            if (hasFeedback)
                throw new Exception("Không thể xóa khóa học đã có đánh giá.");

            //  Soft delete lessons
            var lessons = await _context.Lessons
                .Where(l => l.CourseId == course.Id && !l.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var lesson in lessons)
            {
                lesson.IsDeleted = true;
                lesson.DeletedAt = DateTime.UtcNow;
            }

            //  Soft delete course
            course.IsDeleted = true;
            course.DeletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Course {CourseId} deleted by user {UserId}",
                course.Id,
                userId);

            return course.Id;
        }
    }
}