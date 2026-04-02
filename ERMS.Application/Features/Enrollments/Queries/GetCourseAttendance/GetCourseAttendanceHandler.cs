using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.Enrollments.Queries.GetCourseAttendance
{
    public sealed class GetCourseAttendanceHandler
        : IRequestHandler<GetCourseAttendanceQuery, List<CourseAttendanceDto>>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<GetCourseAttendanceHandler> _logger;

        public GetCourseAttendanceHandler(
            IERMSDbContext context,
            ICurrentUserService currentUserService,
            ILogger<GetCourseAttendanceHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<List<CourseAttendanceDto>> Handle(
            GetCourseAttendanceQuery request,
            CancellationToken cancellationToken)
        {
            var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();

            if (enterpriseId == null)
                throw new Exception("Người dùng không thuộc doanh nghiệp nào.");

            var course = await _context.Courses
                .FirstOrDefaultAsync(c =>
                    c.Id == request.CourseId &&
                    c.EnterpriseId == enterpriseId &&
                    !c.IsDeleted,
                    cancellationToken);

            if (course == null)
                throw new Exception("Không tìm thấy khóa học.");

            var enrollments = await _context.Enrollments
                .Where(e =>
                    e.CourseId == request.CourseId &&
                    !e.IsDeleted)
                .Include(e => e.Employee)
                .Select(e => new CourseAttendanceDto
                {
                    EnrollmentId = e.Id,
                    EmployeeId = e.EmployeeId,
                    EmployeeName = e.Employee.User.FullName,
                    EmployeeCode = e.Employee.EmployeeCode,
                    Status = e.Status,
                    Progress = e.Progress
                })
                .ToListAsync(cancellationToken);

            _logger.LogInformation(
                "Lấy danh sách điểm danh khóa học {CourseId}",
                request.CourseId);

            return enrollments;
        }
    }
}