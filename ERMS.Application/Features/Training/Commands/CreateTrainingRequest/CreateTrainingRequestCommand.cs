using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.Application.Features.Training.Commands.CreateTrainingRequest
{
    public sealed class CreateTrainingRequestCommand : IRequest<Guid>
    {
        public Guid? TrainingPlanId { get; set; }
        public Guid RequestedById { get; set; }
        public string Subject { get; set; } = null!;
        public string? Urgency { get; set; }
        public string? Description { get; set; }
        public string? TargetAudience { get; set; }
        public int? EstimatedParticipants { get; set; }
        public decimal? EstimatedBudget { get; set; }
        public string? ReviewNote { get; set; }
    }
}
