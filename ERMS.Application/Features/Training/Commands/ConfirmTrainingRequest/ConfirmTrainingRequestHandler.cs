using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Training.Commands.ConfirmTrainingRequest
{
    public sealed class ConfirmTrainingRequestHandler
        : IRequestHandler<ConfirmTrainingRequestCommand, bool>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<ConfirmTrainingRequestHandler> _logger;

        public ConfirmTrainingRequestHandler(
            IERMSDbContext context,
            ICurrentUserService currentUserService,
            ILogger<ConfirmTrainingRequestHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<bool> Handle(
            ConfirmTrainingRequestCommand request,
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

            if (trainingRequest.Status == "Confirmed")
                throw new Exception("Request already confirmed");

            // ✅ Confirm request
            trainingRequest.Status = "Confirmed";
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