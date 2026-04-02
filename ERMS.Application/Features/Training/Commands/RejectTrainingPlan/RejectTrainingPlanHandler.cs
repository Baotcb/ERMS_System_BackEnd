using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Training.Commands.RejectTrainingPlan
{
    public sealed class RejectTrainingPlanHandler
        : IRequestHandler<RejectTrainingPlanCommand, bool>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<RejectTrainingPlanHandler> _logger;

        public RejectTrainingPlanHandler(
            IERMSDbContext context,
            ICurrentUserService currentUserService,
            ILogger<RejectTrainingPlanHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<bool> Handle(
            RejectTrainingPlanCommand request,
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
                throw new Exception("Kế hoạch đã được phê duyệt, không thể từ chối");

            //   Reject
            plan.Status = "Rejected";
            plan.ApprovedById = userId.Value;
            plan.ApprovedAt = DateTime.UtcNow;
            plan.ReviewNote = request.ReviewNote;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "TrainingPlan {PlanId} rejected by {UserId}",
                plan.Id,
                userId);

            return true;
        }
    }
}