using MediatR;
using System;

namespace ERMS.Application.Features.Training.Commands.RejectTrainingPlan
{
    public sealed class RejectTrainingPlanCommand : IRequest<bool>
    {
        public Guid TrainingPlanId { get; set; }
        public string ReviewNote { get; set; } = null!;
    }
}