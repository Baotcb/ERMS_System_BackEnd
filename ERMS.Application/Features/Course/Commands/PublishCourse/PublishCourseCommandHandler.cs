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
                throw new UnauthorizedAccessException("User not authenticated");

            var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();

            if (enterpriseId == null)
                throw new Exception("User does not belong to any enterprise");

            var course = await _context.Courses
                .FirstOrDefaultAsync(c =>
                    c.Id == request.Id &&
                    c.EnterpriseId == enterpriseId &&
                    !c.IsDeleted,
                    cancellationToken);

            if (course == null)
                throw new Exception("Course not found");

            if (course.Status == "Published")
                throw new Exception("Course already published");

            // Optional: kiểm tra có lesson
            var lessonCount = await _context.Lessons
                .CountAsync(l =>
                    l.CourseId == course.Id &&
                    !l.IsDeleted,
                    cancellationToken);

            if (lessonCount == 0)
                throw new Exception("Cannot publish course without lessons");

            course.Status = "Published";
            course.PublishedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Course {CourseId} published by User {UserId}",
                course.Id,
                userId);

            return true;
        }
    }
}