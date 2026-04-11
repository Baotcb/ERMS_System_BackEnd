using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Lessons.Commands.DeleteLesson
{
    public sealed class DeleteLessonHandler : IRequestHandler<DeleteLessonCommand, Guid>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public DeleteLessonHandler(
            IERMSDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Guid> Handle(DeleteLessonCommand request, CancellationToken cancellationToken)
        {
            if (request.Id == Guid.Empty)
                throw new ArgumentException("LessonId không hợp lệ");

            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.Id == request.Id && !l.IsDeleted, cancellationToken);

            if (lesson == null)
                throw new KeyNotFoundException("Không tìm thấy bài học");

            var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();
            var roles = _currentUserService.Roles ?? new string[] { };
            var email = _currentUserService.Email;

            var course = lesson.Course;

            //  quyền
            var isHR = roles.Contains("HR");
            var isSameEnterprise = enterpriseId == course.EnterpriseId;
            var isContentManager = course.ContentManagerEmail == email;

            if (!isHR && !isSameEnterprise && !isContentManager)
                throw new UnauthorizedAccessException("Bạn không có quyền xóa bài học này");

            //  Có progress
            var hasProgress = await _context.LessonProgresses
                .AnyAsync(lp => lp.LessonId == lesson.Id, cancellationToken);

            if (hasProgress)
                throw new Exception("Không thể xóa bài học đã có tiến trình học");

            //  optional: course đã publish
            if (course.PublishedAt.HasValue)
                throw new Exception("Không thể xóa bài học khi khóa học đã publish");

            //  Soft delete
            lesson.IsDeleted = true;
            lesson.DeletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return lesson.Id;
        }
    }
}