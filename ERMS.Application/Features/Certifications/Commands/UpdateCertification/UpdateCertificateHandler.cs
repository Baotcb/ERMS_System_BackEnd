using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Enrollments.Commands.UpdateCertificate
{
    public class UpdateCertificateHandler
        : IRequestHandler<UpdateCertificateCommand, bool>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public UpdateCertificateHandler(
            IERMSDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<bool> Handle(
            UpdateCertificateCommand request,
            CancellationToken cancellationToken)
        {
            // 1. Lấy enrollment
            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(x => x.Id == request.EnrollmentId && !x.IsDeleted, cancellationToken);

            if (enrollment == null)
                throw new Exception("Enrollment không tồn tại");

            // 2. Check quyền (HR hoặc Trainer)
            //var roles = _currentUser.Roles;

            //if (!roles.Contains("HR") && !roles.Contains("Trainer"))
            //    throw new Exception("Bạn không có quyền cấp chứng chỉ");

            // 3. Check hoàn thành khóa học
            if (enrollment.Progress < 100)
                throw new Exception("Chưa hoàn thành khóa học");

            // 4. Update trạng thái nếu chưa completed
            if (enrollment.Status != "Completed")
            {
                enrollment.Status = "Completed";
                enrollment.CompletedAt = DateTime.UtcNow;
            }

            // 5. Update chứng chỉ
            enrollment.CertificateUrl = request.CertificateUrl;
            enrollment.CertificateIssuedAt = DateTime.UtcNow;

            // 6. Save
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}