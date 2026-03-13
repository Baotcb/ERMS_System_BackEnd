using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.Application.Features.Training.Commands.UpdateTrainingRequest
{
    public class UpdateTrainingRequestCommand : IRequest<bool>
    {
        public Guid TrainingRequestId { get; set; }
        public string? Subject { get; set; } = null!;
        public string? Urgency { get; set; }
        public string? Description { get; set; }
        public string? TargetAudience { get; set; }
        public int? EstimatedParticipants { get; set; }
        public decimal? EstimatedBudget { get; set; }
    }
}
