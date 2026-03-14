using MediatR;
using System;
using System.Collections.Generic;

namespace ERMS.Application.Features.Training.Commands.UpdateTrainingPlan
{
    public sealed class UpdateTrainingPlanCommand : IRequest<Guid>
    {
        public Guid Id { get; set; }

        public string PlanName { get; set; } = null!;

        public string PlanCode { get; set; } = null!;

        public string? Description { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public decimal? TotalBudget { get; set; }

        public string? ReviewNote { get; set; }

        public List<Guid> TrainingRequestIds { get; set; } = new();
    }
}