using ERMS.Application.Interface;
using ERMS.Domain.Entities.Training;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Training.Commands.CreateTrainingPlan
{
    public sealed class CreateTrainingPlanHandler
        : IRequestHandler<CreateTrainingPlanCommand, Guid>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<CreateTrainingPlanHandler> _logger;

        public CreateTrainingPlanHandler(
            IERMSDbContext context,
            ICurrentUserService currentUserService,
            ILogger<CreateTrainingPlanHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<Guid> Handle(
    CreateTrainingPlanCommand request,
    CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId
                ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực");

            var enterpriseId = await _currentUserService.GetEnterpriseIdAsync()
                ?? throw new Exception("Người dùng không thuộc doanh nghiệp nào");

            if (request.EndDate < request.StartDate)
                throw new Exception("Ngày kết thúc phải sau ngày bắt đầu");

            if (!request.TrainingRequestIds.Any())
                throw new Exception("Cần có ít nhất một yêu cầu đào tạo");

            //   Check duplicate PlanCode
            var existedCode = await _context.TrainingPlans
                .AnyAsync(p =>
                    p.PlanCode == request.PlanCode &&
                    p.EnterpriseId == enterpriseId &&
                    !p.IsDeleted,
                    cancellationToken);

            if (existedCode)
                throw new Exception("Mã kế hoạch đã tồn tại");

            //   Get Requests
            var requests = await _context.TrainingRequests
                .Where(r =>
                    request.TrainingRequestIds.Contains(r.Id)
                    && r.TrainingPlan == null
                    && r.Status == "Pending"
                    && !r.IsDeleted)
                .ToListAsync(cancellationToken);

            if (requests.Count != request.TrainingRequestIds.Count)
                throw new Exception("Một số yêu cầu đào tạo không hợp lệ");

            //   Transaction
            using var transaction =
                await _context.BeginTransactionAsync(cancellationToken);

            try
            {
                var trainingPlan = new TrainingPlan
                {
                    Id = Guid.CreateVersion7(),
                    EnterpriseId = enterpriseId,
                    PlanName = request.PlanName,
                    PlanCode = request.PlanCode,
                    Description = request.Description,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    TotalBudget = request.TotalBudget,
                    Status = request.Status,
                    ReviewNote = request.ReviewNote,
                    CreatedById = userId,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                _context.TrainingPlans.Add(trainingPlan);

                //   Assign requests → plan
                foreach (var req in requests)
                {
                    req.TrainingPlanId = trainingPlan.Id;
                    req.Status = "AddedToPlan";
                    req.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "TrainingPlan {PlanCode} created with {Count} requests",
                    trainingPlan.PlanCode,
                    requests.Count);

                return trainingPlan.Id;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}