using System;
using System.Collections.Generic;

namespace ERMS.Application.Features.Training.Queries.GetTrainingPlanDetail
{
    public sealed class TrainingPlanDetailDto
    {
        public Guid Id { get; set; }

        public string PlanName { get; set; } = null!;

        public string PlanCode { get; set; } = null!;

        public string? Description { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public decimal? TotalBudget { get; set; }

        public string Status { get; set; } = null!;

        public string? ReviewNote { get; set; }

        public List<TrainingRequestDto> TrainingRequests { get; set; } = new();
    }

    public sealed class TrainingRequestDto
    {
        public Guid Id { get; set; }

        public string Subject { get; set; } = null!;

        public string RequestedByName { get; set; } = null!;

        public string Status { get; set; } = null!;
    }
}