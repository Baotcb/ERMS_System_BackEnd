using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.Certifications.Queries
{
    public sealed class GetMyCertificationsByCourseQueryHandler
        : IRequestHandler<GetMyCertificationsByCourseQuery, List<CertificationDto>>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<GetMyCertificationsByCourseQueryHandler> _logger;

        public GetMyCertificationsByCourseQueryHandler(
            IERMSDbContext context,
            ICurrentUserService currentUserService,
            ILogger<GetMyCertificationsByCourseQueryHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<List<CertificationDto>> Handle(
            GetMyCertificationsByCourseQuery request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (userId == null)
                throw new Exception("Không xác định được người dùng.");

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);

            if (employee == null)
                throw new Exception("Không tìm thấy nhân viên.");

            var result = await _context.Enrollments
                .Where(e =>
                    e.EmployeeId == employee.Id &&
                    e.CourseId == request.CourseId &&
                    !e.IsDeleted &&
                    e.CertificateUrl != null)
                .Include(e => e.Course)
                .Include(e => e.Employee)
        .ThenInclude(emp => emp.User)
                .Select(e => new CertificationDto
                {
                    EnrollmentId = e.Id,
                    CourseId = e.CourseId,
                    CourseName = e.Course.CourseName,
                    EmployeeId = e.EmployeeId,
                    EmployeeName = e.Employee.User.FullName,
                    CertificateUrl = e.CertificateUrl,
                    CertificateIssuedAt = e.CertificateIssuedAt,
                    Progress = e.Progress,
                    Status = e.Status
                })
                .ToListAsync(cancellationToken);

            return result;
        }
    }
}