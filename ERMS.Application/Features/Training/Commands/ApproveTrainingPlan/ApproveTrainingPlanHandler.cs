using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Training.Commands.ApproveTrainingPlan
{
    public sealed class ApproveTrainingPlanHandler
        : IRequestHandler<ApproveTrainingPlanCommand, bool>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<ApproveTrainingPlanHandler> _logger;

        public ApproveTrainingPlanHandler(
            IERMSDbContext context,
            ICurrentUserService currentUserService,
            ILogger<ApproveTrainingPlanHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<bool> Handle(
            ApproveTrainingPlanCommand request,
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

            if (plan.Status == "Approved")
                throw new Exception("Kế hoạch đã được phê duyệt");

            // ✅ Approve
            plan.Status = "Approved";
            plan.ApprovedById = userId.Value;
            plan.ApprovedAt = DateTime.UtcNow;
            plan.ReviewNote = request.ReviewNote;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "TrainingPlan {PlanId} approved by {UserId}",
                plan.Id,
                userId);

            return true;
        }
    }
}