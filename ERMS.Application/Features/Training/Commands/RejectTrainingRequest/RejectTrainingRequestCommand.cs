using MediatR;
using System;

namespace ERMS.Application.Features.Training.Commands.ConfirmTrainingRequest
{
    public sealed class RejectTrainingRequestCommand : IRequest<bool>
    {
        public Guid TrainingRequestId { get; set; }
        public string ReviewNote { get; set; }
    }
}