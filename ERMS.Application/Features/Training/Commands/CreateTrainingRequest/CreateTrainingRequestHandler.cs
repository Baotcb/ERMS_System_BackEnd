using ERMS.Application.Interface;
using ERMS.Domain.Entities.Training;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Training.Commands.CreateTrainingRequest
{
    public sealed class CreateTrainingRequestHandler
        : IRequestHandler<CreateTrainingRequestCommand, Guid>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<CreateTrainingRequestHandler> _logger;

        public CreateTrainingRequestHandler(
            IERMSDbContext context,
            ICurrentUserService currentUserService,
            ILogger<CreateTrainingRequestHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<Guid> Handle(
            CreateTrainingRequestCommand request,
            CancellationToken cancellationToken)
        {
           

            var userId = _currentUserService.UserId;
            if (userId == null)
                throw new UnauthorizedAccessException("User not authenticated");

            // Validate enterprise
            var enterpriseExists = await _context.Enterprises
                .AnyAsync(e => e.Id == request.EnterpriseId && !e.IsDeleted, cancellationToken);

            if (!enterpriseExists)
                throw new Exception("Doanh nghiệp không tồn tại");

            // Validate department
            var departmentExists = await _context.Departments
                .AnyAsync(d => d.Id == request.DepartmentId
                            && d.EnterpriseId == request.EnterpriseId
                            && !d.IsDeleted, cancellationToken);

            if (!departmentExists)
                throw new Exception("Phòng ban không tồn tại");

            // Validate TrainingPlan nếu có
            if (request.TrainingPlanId.HasValue)
            {
                var planExists = await _context.TrainingPlans
                    .AnyAsync(p => p.Id == request.TrainingPlanId.Value
                                && p.EnterpriseId == request.EnterpriseId
                                && !p.IsDeleted, cancellationToken);

                if (!planExists)
                    throw new Exception("Kế hoạch đào tạo không tồn tại");
            }

            var trainingRequest = new TrainingRequest
            {
                Id = Guid.CreateVersion7(),
                EnterpriseId = request.EnterpriseId,
                TrainingPlanId = request.TrainingPlanId,
                DepartmentId = request.DepartmentId,
                RequestedById = userId.Value,
                Subject = request.Subject,
                Urgency = request.Urgency,
                Description = request.Description,
                TargetAudience = request.TargetAudience,
                EstimatedParticipants = request.EstimatedParticipants,
                EstimatedBudget = request.EstimatedBudget,
                Status = "Pending",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.TrainingRequests.Add(trainingRequest);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Created training request {Subject} by user {UserId} in enterprise {EnterpriseId}",
                trainingRequest.Subject,
                trainingRequest.RequestedById,
                trainingRequest.EnterpriseId);

            return trainingRequest.Id;
        }
    }
}