using ERMS.Application.Interface;
using ERMS.Domain.Constants.Training;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Training.Commands.CloseTrainingPlan
{
    public sealed class CloseTrainingPlanHandler
        : IRequestHandler<CloseTrainingPlanCommand, bool>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<CloseTrainingPlanHandler> _logger;

        public CloseTrainingPlanHandler(
            IERMSDbContext context,
            ICurrentUserService currentUserService,
            ILogger<CloseTrainingPlanHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<bool> Handle(
    CloseTrainingPlanCommand request,
    CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (userId == null)
                throw new UnauthorizedAccessException();

            var enterpriseId =
                await _currentUserService.GetEnterpriseIdAsync();

            var plan = await _context.TrainingPlans
                .FirstOrDefaultAsync(p =>
                    p.Id == request.TrainingPlanId &&
                    p.EnterpriseId == enterpriseId &&
                    !p.IsDeleted,
                    cancellationToken);

            if (plan == null)
                throw new Exception("Không tìm thấy kế hoạch đào tạo");

            if (plan.Status == "Closed")
                throw new Exception("Kế hoạch đã được đóng");

            if (plan.Status != "Approved")
                throw new Exception("Chỉ có thể đóng kế hoạch đã được phê duyệt");

            // ===== 1. Close plan =====
            plan.Status = "Closed";

            // ===== 2. Complete Training Requests =====
            var requests = await _context.TrainingRequests
                .Where(r =>
                    r.TrainingPlanId == plan.Id &&
                    !r.IsDeleted &&
                    r.Status != TrainingRequestStatus.Completed)
                .ToListAsync(cancellationToken);

            foreach (var req in requests)
            {
                req.Status = TrainingRequestStatus.Completed;
                req.UpdatedAt = DateTime.UtcNow;
            }

            // ===== 3. Close related Courses =====
            var courses = await _context.Courses
                .Where(c =>
                    c.TrainingPlanId == plan.Id &&
                    !c.IsDeleted &&
                    c.Status != "Closed")
                .ToListAsync(cancellationToken);

            foreach (var course in courses)
            {
                course.Status = "Closed";
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "TrainingPlan {PlanId} closed by {UserId}. {RequestCount} requests completed, {CourseCount} courses closed",
                plan.Id,
                userId,
                requests.Count,
                courses.Count);

            return true;
        }
    }
}