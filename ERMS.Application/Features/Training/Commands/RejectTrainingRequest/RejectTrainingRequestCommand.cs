using ERMS.Domain.Constants.Training;
using MediatR;
using System;

namespace ERMS.Application.Features.Training.Commands.ConfirmTrainingRequest
{
    public sealed class RejectTrainingRequestCommand : IRequest<bool>
    {
        public Guid TrainingRequestId { get; set; }
        public string Status { get; set; } = TrainingRequestStatus.Rejected;

        public string ReviewNote { get; set; }
    }
}