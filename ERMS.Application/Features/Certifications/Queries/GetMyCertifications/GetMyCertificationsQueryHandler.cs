using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.Certifications.Queries.GetMyCertifications
{
    public sealed class GetMyCertificationsQueryHandler
        : IRequestHandler<GetMyCertificationsQuery, List<CertificationDto>>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<GetMyCertificationsQueryHandler> _logger;

        public GetMyCertificationsQueryHandler(
            IERMSDbContext context,
            ICurrentUserService currentUserService,
            ILogger<GetMyCertificationsQueryHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<List<CertificationDto>> Handle(
            GetMyCertificationsQuery request,
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
        e.Employee.UserId == userId &&
        !e.IsDeleted &&
        e.CertificateUrl != null)
    .Include(e => e.Course)
    .Include(e => e.Employee)
        .ThenInclude(emp => emp.User) // 👈 BẮT BUỘC
    .Select(e => new CertificationDto
    {
        EnrollmentId = e.Id,
        CourseId = e.CourseId,
        CourseName = e.Course.CourseName,
        EmployeeId = e.EmployeeId,
        EmployeeName = e.Employee.User.FullName, // giờ mới safe
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