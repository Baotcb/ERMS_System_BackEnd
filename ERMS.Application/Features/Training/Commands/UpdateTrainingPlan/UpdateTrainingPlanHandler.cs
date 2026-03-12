using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.Training.Commands.UpdateTrainingPlan
{
    public sealed class UpdateTrainingPlanHandler
        : IRequestHandler<UpdateTrainingPlanCommand, Guid>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<UpdateTrainingPlanHandler> _logger;

        public UpdateTrainingPlanHandler(
            IERMSDbContext context,
            ICurrentUserService currentUserService,
            ILogger<UpdateTrainingPlanHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<Guid> Handle(
            UpdateTrainingPlanCommand request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId
                ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực");

            var enterpriseId = await _currentUserService.GetEnterpriseIdAsync()
                ?? throw new Exception("Người dùng không thuộc doanh nghiệp nào");

            if (request.EndDate < request.StartDate)
                throw new Exception("Ngày kết thúc phải sau ngày bắt đầu");

            var plan = await _context.TrainingPlans
                .FirstOrDefaultAsync(p =>
                    p.Id == request.Id &&
                    p.EnterpriseId == enterpriseId &&
                    !p.IsDeleted,
                    cancellationToken);

            if (plan == null)
                throw new Exception("Không tìm thấy kế hoạch đào tạo");

            if (plan.Status == "Approved")
                throw new Exception("Không thể cập nhật kế hoạch đã được phê duyệt");

            // Check duplicate PlanCode
            if (plan.PlanCode != request.PlanCode)
            {
                var existedCode = await _context.TrainingPlans
                    .AnyAsync(p =>
                        p.PlanCode == request.PlanCode &&
                        p.Id != request.Id &&
                        p.EnterpriseId == enterpriseId &&
                        !p.IsDeleted,
                        cancellationToken);

                if (existedCode)
                    throw new Exception("Mã kế hoạch đã tồn tại");
            }

            using var transaction =
                await _context.BeginTransactionAsync(cancellationToken);

            try
            {
                bool isUpdated = false;

                if (plan.PlanName != request.PlanName)
                {
                    plan.PlanName = request.PlanName;
                    isUpdated = true;
                }

                if (plan.PlanCode != request.PlanCode)
                {
                    plan.PlanCode = request.PlanCode;
                    isUpdated = true;
                }

                if (plan.Description != request.Description)
                {
                    plan.Description = request.Description;
                    isUpdated = true;
                }

                if (plan.StartDate != request.StartDate)
                {
                    plan.StartDate = request.StartDate;
                    isUpdated = true;
                }

                if (plan.EndDate != request.EndDate)
                {
                    plan.EndDate = request.EndDate;
                    isUpdated = true;
                }

                if (plan.TotalBudget != request.TotalBudget)
                {
                    plan.TotalBudget = request.TotalBudget;
                    isUpdated = true;
                }

                if (plan.ReviewNote != request.ReviewNote)
                {
                    plan.ReviewNote = request.ReviewNote;
                    isUpdated = true;
                }

                if (isUpdated)
                {
                    plan.UpdatedAt = DateTime.UtcNow;
                }

                // ===== Update TrainingRequests only if changed =====

                var currentRequestIds = await _context.TrainingRequests
                    .Where(r => r.TrainingPlanId == plan.Id)
                    .Select(r => r.Id)
                    .ToListAsync(cancellationToken);

                var newRequestIds = request.TrainingRequestIds;

                var toRemove = currentRequestIds.Except(newRequestIds).ToList();
                var toAdd = newRequestIds.Except(currentRequestIds).ToList();

                if (toRemove.Any())
                {
                    var removeRequests = await _context.TrainingRequests
                        .Where(r => toRemove.Contains(r.Id))
                        .ToListAsync(cancellationToken);

                    foreach (var r in removeRequests)
                    {
                        r.TrainingPlanId = null;
                        r.Status = "Pending";
                        r.UpdatedAt = DateTime.UtcNow;
                    }
                }

                if (toAdd.Any())
                {
                    var addRequests = await _context.TrainingRequests
                        .Where(r => toAdd.Contains(r.Id) && !r.IsDeleted)
                        .ToListAsync(cancellationToken);

                    foreach (var r in addRequests)
                    {
                        r.TrainingPlanId = plan.Id;
                        r.Status = "AddedToPlan";
                        r.UpdatedAt = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "TrainingPlan {PlanId} updated by {UserId}",
                    plan.Id,
                    userId);

                return plan.Id;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}