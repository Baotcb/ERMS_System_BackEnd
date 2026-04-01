using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Lessons.Commands.UpdateLesson
{
    public sealed class UpdateLessonHandler : IRequestHandler<UpdateLessonCommand, Guid>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public UpdateLessonHandler(
            IERMSDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Guid> Handle(UpdateLessonCommand request, CancellationToken cancellationToken)
        {
            // 1. Validate cơ bản (nếu chưa dùng FluentValidation)
            if (request.Id == Guid.Empty)
                throw new ArgumentException("LessonId không hợp lệ");

            if (string.IsNullOrWhiteSpace(request.LessonTitle))
                throw new ArgumentException("Tiêu đề không được để trống");

            if (request.OrderIndex < 0)
                throw new ArgumentException("OrderIndex không hợp lệ");

            if (request.EstimatedMinutes.HasValue && request.EstimatedMinutes < 0)
                throw new ArgumentException("EstimatedMinutes không hợp lệ");

            // 2. Lấy lesson + course
            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(
                    l => l.Id == request.Id && !l.IsDeleted,
                    cancellationToken);

            if (lesson == null)
                throw new KeyNotFoundException("Không tìm thấy bài học");

            // 3. Lấy thông tin user
            var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();
            var roles = _currentUserService.Roles ?? new string[] { };
            var email = _currentUserService.Email;

            var course = lesson.Course;

            // 4. Check quyền
            var isHR = roles.Contains("HR");
            var isSameEnterprise = enterpriseId == course.EnterpriseId;
            var isContentManager = course.ContentManagerEmail == email;

            if (!isHR && !isSameEnterprise && !isContentManager)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa bài học này");
            }

            // 5. Validate theo ContentType (rất quan trọng)
            switch (request.ContentType?.ToLower())
            {
                case "video":
                    if (string.IsNullOrWhiteSpace(request.VideoUrl))
                        throw new ArgumentException("VideoUrl là bắt buộc với ContentType = Video");
                    break;

                case "document":
                    if (string.IsNullOrWhiteSpace(request.DocumentUrl))
                        throw new ArgumentException("DocumentUrl là bắt buộc với ContentType = Document");
                    break;

                case "link":
                    if (string.IsNullOrWhiteSpace(request.ExternalLinkUrl))
                        throw new ArgumentException("ExternalLinkUrl là bắt buộc với ContentType = Link");
                    break;

                case "text":
                    if (string.IsNullOrWhiteSpace(request.Content))
                        throw new ArgumentException("Content là bắt buộc với ContentType = Text");
                    break;
            }

            // 6. Update dữ liệu
            lesson.LessonTitle = request.LessonTitle;
            lesson.Description = request.Description;
            lesson.OrderIndex = request.OrderIndex;
            lesson.ContentType = request.ContentType;

            lesson.VideoUrl = request.VideoUrl;
            lesson.VideoDurationMinutes = request.VideoDurationMinutes;

            lesson.DocumentUrl = request.DocumentUrl;
            lesson.ExternalLinkUrl = request.ExternalLinkUrl;

            lesson.Content = request.Content;

            lesson.IsPreview = request.IsPreview;
            lesson.EstimatedMinutes = request.EstimatedMinutes;

            lesson.UpdatedAt = DateTime.UtcNow;

            // 7. Save DB
            await _context.SaveChangesAsync(cancellationToken);

            // 8. Return kết quả
            return lesson.Id;
        }
    }
}