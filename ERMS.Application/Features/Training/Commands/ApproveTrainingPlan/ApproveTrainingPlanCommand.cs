using MediatR;
using System;

namespace ERMS.Application.Features.Training.Commands.ApproveTrainingPlan
{
    public sealed class ApproveTrainingPlanCommand : IRequest<bool>
    {
        public Guid TrainingPlanId { get; set; }
        public string? ReviewNote { get; set; }
    }
}