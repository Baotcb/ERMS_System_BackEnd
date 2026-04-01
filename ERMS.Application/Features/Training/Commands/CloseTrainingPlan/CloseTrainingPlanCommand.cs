using MediatR;
using System;

namespace ERMS.Application.Features.Training.Commands.CloseTrainingPlan
{
    public sealed class CloseTrainingPlanCommand : IRequest<bool>
    {
        public Guid TrainingPlanId { get; set; }

        public string? ClosingNote { get; set; }
    }
}