using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Training.Commands.ConfirmTrainingRequest
{
    public sealed class RejectTrainingRequestHandler
        : IRequestHandler<RejectTrainingRequestCommand, bool>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<RejectTrainingRequestHandler> _logger;

        public RejectTrainingRequestHandler(
            IERMSDbContext context,
            ICurrentUserService currentUserService,
            ILogger<RejectTrainingRequestHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<bool> Handle(
            RejectTrainingRequestCommand request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (userId == null)
                throw new UnauthorizedAccessException();

            var enterpriseId =
                await _currentUserService.GetEnterpriseIdAsync();

            var trainingRequest = await _context.TrainingRequests
                .FirstOrDefaultAsync(r =>
                    r.Id == request.TrainingRequestId &&
                    r.EnterpriseId == enterpriseId &&
                    !r.IsDeleted,
                    cancellationToken);

            if (trainingRequest == null)
                throw new Exception("Training request not found");

            if (trainingRequest.Status == "AddedToPlan")
                throw new Exception("Request already confirmed");

            if (trainingRequest.Status != "Pending")
                throw new Exception("Request already rejected");

            // ✅ Confirm request
            trainingRequest.Status = request.Status;
            trainingRequest.ReviewNote = request.ReviewNote;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "TrainingRequest {RequestId} confirmed by {UserId}",
                trainingRequest.Id,
                userId);

            return true;
        }
    }
}