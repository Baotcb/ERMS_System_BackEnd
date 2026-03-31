using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
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
            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.Id == request.Id && !l.IsDeleted, cancellationToken);

            if (lesson == null)
                throw new KeyNotFoundException("Không tìm thấy bài học");

            var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();
            var roles = _currentUserService.Roles;
            var email = _currentUserService.Email;

            var course = lesson.Course;

            if (!roles.Contains("HR") &&
                enterpriseId != course.EnterpriseId &&
                course.ContentManagerEmail != email)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa bài học này");
            }

            // Update
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

            await _context.SaveChangesAsync(cancellationToken);

            return lesson.Id;
        }
    }
}