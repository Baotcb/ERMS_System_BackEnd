using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.Training.Commands.DeleteTrainingPlan
{
    public sealed class DeleteTrainingPlanHandler
        : IRequestHandler<DeleteTrainingPlanCommand, Guid>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<DeleteTrainingPlanHandler> _logger;

        public DeleteTrainingPlanHandler(
            IERMSDbContext context,
            ICurrentUserService currentUserService,
            ILogger<DeleteTrainingPlanHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<Guid> Handle(
            DeleteTrainingPlanCommand request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId
                ?? throw new UnauthorizedAccessException("Người dùng chưa đăng nhập");

            var enterpriseId = await _currentUserService.GetEnterpriseIdAsync()
                ?? throw new Exception("Người dùng không thuộc doanh nghiệp");

            var roles = _currentUserService.Roles ?? new string[] { };

            var plan = await _context.TrainingPlans
                .Include(p => p.Courses)
                .Include(p => p.TrainingRequests)
                .FirstOrDefaultAsync(p =>
                    p.Id == request.Id &&
                    p.EnterpriseId == enterpriseId &&
                    !p.IsDeleted,
                    cancellationToken);

            if (plan == null)
                throw new KeyNotFoundException("Không tìm thấy kế hoạch đào tạo");

            //  Check quyền (HR hoặc creator)
            var isHR = roles.Contains(AppRoles.HRManager);
            var isOwner = plan.CreatedById == userId;

            if (!isHR && !isOwner)
                throw new UnauthorizedAccessException("Bạn không có quyền xóa kế hoạch này");

            //  Approved thì cấm xóa
            if (plan.Status == "Approved")
                throw new Exception("Không thể xóa kế hoạch đã được phê duyệt");

            //  Có course
            var hasCourses = await _context.Courses
                .AnyAsync(c => c.TrainingPlanId == plan.Id && !c.IsDeleted, cancellationToken);

            if (hasCourses)
                throw new Exception("Không thể xóa kế hoạch đã có khóa học");

            //  Có enrollment (gián tiếp qua course)
            var hasEnrollment = await _context.Enrollments
                .AnyAsync(e => e.Course.TrainingPlanId == plan.Id && !e.IsDeleted, cancellationToken);

            if (hasEnrollment)
                throw new Exception("Không thể xóa kế hoạch đã có học viên");

            //  TrainingRequest không hợp lệ
            var invalidRequests = await _context.TrainingRequests
                .Where(r => r.TrainingPlanId == plan.Id)
                .Where(r => r.Status != "Pending")
                .AnyAsync(cancellationToken);

            if (invalidRequests)
                throw new Exception("Không thể xóa vì có yêu cầu không ở trạng thái chờ");

            // ===== Update TrainingRequests =====
            var requests = await _context.TrainingRequests
                .Where(r => r.TrainingPlanId == plan.Id)
                .ToListAsync(cancellationToken);

            foreach (var r in requests)
            {
                r.TrainingPlanId = null;
                r.Status = "Pending";
                r.UpdatedAt = DateTime.UtcNow;
            }

            // ===== Soft delete =====
            plan.IsDeleted = true;
            plan.DeletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "TrainingPlan {PlanId} deleted by {UserId}",
                plan.Id,
                userId);

            return plan.Id;
        }
    }
}