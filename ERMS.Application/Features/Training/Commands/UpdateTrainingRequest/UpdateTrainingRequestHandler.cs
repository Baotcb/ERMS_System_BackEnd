using ERMS.Application.Interface;
using ERMS.Domain.Entities.Training;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace ERMS.Application.Features.Training.Commands.UpdateTrainingRequest
{
    public sealed class UpdateTrainingRequestHandler
        : IRequestHandler<UpdateTrainingRequestCommand, bool>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<UpdateTrainingRequestHandler> _logger;

        public UpdateTrainingRequestHandler(
            IERMSDbContext context,
            ICurrentUserService currentUserService,
            ILogger<UpdateTrainingRequestHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<bool> Handle(
            UpdateTrainingRequestCommand request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId
                ?? throw new UnauthorizedAccessException();

            var trainingRequest = await _context.TrainingRequests
                .FirstOrDefaultAsync(x =>
                    x.Id == request.TrainingRequestId &&
                    !x.IsDeleted,
                    cancellationToken);

            if (trainingRequest == null)
                throw new Exception("Không tìm thấy yêu cầu đào tạo");

            // chỉ owner được sửa
            if (trainingRequest.RequestedById != userId)
                throw new Exception("Bạn không có quyền cập nhật yêu cầu này");

            // CHỈ cho sửa khi chưa được phân bổ, tức là Pending, Rejected, hoặc NeedRevision
            var editableStatuses = new[] { "Pending", "Rejected", "NeedRevision" };

            if (!editableStatuses.Contains(trainingRequest.Status))
            {
                throw new Exception(
                    "Chỉ có thể cập nhật các yêu cầu đang chờ duyệt, đã bị từ chối hoặc cần chỉnh sửa.");
            }

            // Update fields
            trainingRequest.Subject = request.Subject ?? trainingRequest.Subject;
            trainingRequest.Urgency = request.Urgency ?? trainingRequest.Urgency;
            trainingRequest.Description = request.Description ?? trainingRequest.Description;
            trainingRequest.TargetAudience = request.TargetAudience ?? trainingRequest.TargetAudience;
            trainingRequest.EstimatedParticipants =
                request.EstimatedParticipants ?? trainingRequest.EstimatedParticipants;
            trainingRequest.EstimatedBudget =
                request.EstimatedBudget ?? trainingRequest.EstimatedBudget;

            // Reset review info
            trainingRequest.ReviewNote = null;

            // quay lại Pending để HR duyệt lại
            trainingRequest.Status = "Pending";
            trainingRequest.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Training request {Id} resubmitted by user {UserId}",
                trainingRequest.Id,
                userId);

            return true;
        }
    }
}
