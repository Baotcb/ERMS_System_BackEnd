using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.Application.Features.Training.Commands.CreateTrainingPlan
{
    public sealed class CreateTrainingPlanCommand : IRequest<Guid>
    {
        public string PlanName { get; set; } = null!;
        public string PlanCode { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal? TotalBudget { get; set; }
        public string Status { get; set; } = "Draft";
        public string? ReviewNote { get; set; }

        public List<Guid> TrainingRequestIds { get; set; } = new();
    }
}
