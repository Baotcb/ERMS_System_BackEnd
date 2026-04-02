using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.Enrollments.Commands.UpdateAttendance
{
    public sealed class UpdateAttendanceHandler
        : IRequestHandler<UpdateAttendanceCommand, Guid>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<UpdateAttendanceHandler> _logger;

        public UpdateAttendanceHandler(
            IERMSDbContext context,
            ICurrentUserService currentUserService,
            ILogger<UpdateAttendanceHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<Guid> Handle(
            UpdateAttendanceCommand request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (userId == null)
                throw new UnauthorizedAccessException("Người dùng chưa đăng nhập.");

            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e =>
                    e.Id == request.EnrollmentId &&
                    !e.IsDeleted,
                    cancellationToken);

            if (enrollment == null)
                throw new Exception("Không tìm thấy học viên trong khóa học.");

            enrollment.Status = request.Status;
            enrollment.LastAccessedAt = DateTime.UtcNow;
            enrollment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Điểm danh học viên {EnrollmentId} thành {Status} bởi {UserId}",
                enrollment.Id,
                request.Status,
                userId);

            return enrollment.Id;
        }
    }
}