using ERMS.Application.Interface;
using ERMS.Domain.Entities.Training;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.Courses.Commands.CreateCourse
{
    public sealed class CreateCourseHandler
        : IRequestHandler<CreateCourseCommand, Guid>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<CreateCourseHandler> _logger;

        public CreateCourseHandler(
            IERMSDbContext context,
            ICurrentUserService currentUserService,
            ILogger<CreateCourseHandler> logger)
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

            // Lấy EnterpriseId tự động
            var enterpriseId =
                await _currentUserService.GetEnterpriseIdAsync();

            if (enterpriseId == null)
                throw new Exception("Người dùng không thuộc doanh nghiệp nào");

            // Check duplicate CourseCode
            var existedCode = await _context.Courses
                .AnyAsync(c =>
                    c.CourseCode == request.CourseCode &&
                    c.EnterpriseId == enterpriseId &&
                    !c.IsDeleted,
                    cancellationToken);

            if (existedCode)
                throw new Exception("Mã khóa học đã tồn tại");

            var existedTime = await _context.Courses
                .AnyAsync(c =>
                    c.StartTime == request.StartTime &&
                    c.EnterpriseId == enterpriseId &&
                    !c.IsDeleted,
                    cancellationToken);

            if (existedTime)
                throw new Exception("Thời gian học bị trùng");

            // Create Course
            var course = new Course
            {
                Id = Guid.CreateVersion7(),
                EnterpriseId = enterpriseId.Value,
                TrainingPlanId = request.TrainingPlanId,
                CourseName = request.CourseName,
                CourseCode = request.CourseCode,
                Description = request.Description,
                StartTime = request.StartTime,
                IsOnline = request.IsOnline,
                Location = request.IsOnline ? null : request.Location,
                ThumbnailUrl = request.ThumbnailUrl,
                TrainerEmail= request.TrainerEmail,
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

            // Auto-detect: trainer nội bộ hay bên ngoài?
            var trainerEmail = request.TrainerEmail?.Trim().ToLower() ?? "";
            var internalEmployee = await _context.Employees
                .FirstOrDefaultAsync(e => 
                    e.EnterpriseId == enterpriseId && 
                    !e.IsDeleted && 
                    e.User.Email.ToLower() == trainerEmail, 
                    cancellationToken);

            if (internalEmployee != null)
            {
                course.ContentManagerEmail = request.TrainerEmail;
                
                // Trở thành Trainer thì bật cờ IsTrainer = true để họ thấy tab Giảng dạy bên FE
                if (!internalEmployee.IsTrainer)
                {
                    internalEmployee.IsTrainer = true;
                    _context.Employees.Update(internalEmployee);
                }
            }
            else
            {
                // Trainer ngoài enterprise → HR (người tạo) sẽ quản lý nội dung
                var currentUserEmail = _currentUserService.Email;
                course.ContentManagerEmail = currentUserEmail;
                _logger.LogInformation(
                    "External trainer detected ({TrainerEmail}). Content manager assigned to HR: {ContentManager}",
                    request.TrainerEmail, currentUserEmail);
            }

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