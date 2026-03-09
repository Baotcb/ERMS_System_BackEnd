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

            // Check duplicate CourseCode
            var existedCode = await _context.Courses
                .AnyAsync(c =>
                    c.CourseCode == request.CourseCode &&
                    c.Id != request.Id &&
                    c.EnterpriseId == enterpriseId &&
                    !c.IsDeleted,
                    cancellationToken);

            if (existedCode)
                throw new Exception("CourseCode already exists");

            // Validate Trainer
            var trainer = await _context.Employees
                .FirstOrDefaultAsync(e =>
                    e.UserId == request.TrainerId &&
                    e.EnterpriseId == enterpriseId &&
                    !e.IsDeleted,
                    cancellationToken);

            if (trainer == null)
                throw new Exception("Trainer not found");

            if (!trainer.IsTrainer)
                throw new Exception("Employee is not a trainer");

            // Update fields
            course.TrainingPlanId = request.TrainingPlanId;
            course.CourseName = request.CourseName;
            course.CourseCode = request.CourseCode;
            course.Description = request.Description;
            course.ThumbnailUrl = request.ThumbnailUrl;
            course.TrainerId = trainer.Id;
            course.DurationMinutes = request.DurationMinutes;
            course.Level = request.Level;
            course.IsMandatory = request.IsMandatory;
            course.MaxEnrollments = request.MaxEnrollments;
            course.EnrollmentDeadline = request.EnrollmentDeadline;
            course.CompletionCriteria = request.CompletionCriteria;

            course.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Course {CourseId} updated by User {UserId}",
                course.Id,
                userId);

            return course.Id;
        }
    }
}