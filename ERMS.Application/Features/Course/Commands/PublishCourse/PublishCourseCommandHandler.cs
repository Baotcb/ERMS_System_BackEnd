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

        public PublishCourseCommandHandler(
            IERMSDbContext context,
            ICurrentUserService currentUserService,
            ILogger<PublishCourseCommandHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
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

            _logger.LogInformation(
                "Khóa học {CourseId} đã được xuất bản bởi người dùng {UserId}",
                course.Id,
                userId);

            return true;
        }
    }
}