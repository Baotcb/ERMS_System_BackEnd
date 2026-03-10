using ERMS.Application.Interface;
using ERMS.Domain.Entities.Training;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.Courses.Commands.CreateCourse
{
    public sealed class CreateCourseCommandHandler
        : IRequestHandler<CreateCourseCommand, Guid>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<CreateCourseCommandHandler> _logger;

        public CreateCourseCommandHandler(
            IERMSDbContext context,
            ICurrentUserService currentUserService,
            ILogger<CreateCourseCommandHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<Guid> Handle(
            CreateCourseCommand request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (userId == null)
                throw new UnauthorizedAccessException("Người dùng chưa được xác thực");

            // ✅ Lấy EnterpriseId tự động
            var enterpriseId =
                await _currentUserService.GetEnterpriseIdAsync();

            if (enterpriseId == null)
                throw new Exception("Người dùng không thuộc doanh nghiệp nào");

            // ✅ Check duplicate CourseCode
            var existedCode = await _context.Courses
                .AnyAsync(c =>
                    c.CourseCode == request.CourseCode &&
                    c.EnterpriseId == enterpriseId &&
                    !c.IsDeleted,
                    cancellationToken);

            if (existedCode)
                throw new Exception("Mã khóa học đã tồn tại");

            // ✅ Validate Trainer tồn tại & là Trainer
            var trainer = await _context.Employees
                .FirstOrDefaultAsync(e =>
                    e.UserId == request.TrainerId &&
                    e.EnterpriseId == enterpriseId &&
                    !e.IsDeleted,
                    cancellationToken);

            if (trainer == null)
                throw new Exception("Không tìm thấy giảng viên");

            if (!trainer.IsTrainer)
                throw new Exception("Nhân viên này không phải là giảng viên");

            // ✅ Create Course
            var course = new Course
            {
                Id = Guid.CreateVersion7(),
                EnterpriseId = enterpriseId.Value,
                TrainingPlanId = request.TrainingPlanId,
                CourseName = request.CourseName,
                CourseCode = request.CourseCode,
                Description = request.Description,
                ThumbnailUrl = request.ThumbnailUrl,
                TrainerId = trainer.Id,
                DurationMinutes = request.DurationMinutes,
                Level = request.Level,
                IsMandatory = request.IsMandatory,
                MaxEnrollments = request.MaxEnrollments,
                EnrollmentDeadline = request.EnrollmentDeadline,
                CompletionCriteria = request.CompletionCriteria,

                Status = "Draft",
             
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _context.Courses.Add(course);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Course {CourseCode} created by User {UserId} in Enterprise {EnterpriseId}",
                course.CourseCode,
                userId,
                enterpriseId);

            return course.Id;
        }
    }
}