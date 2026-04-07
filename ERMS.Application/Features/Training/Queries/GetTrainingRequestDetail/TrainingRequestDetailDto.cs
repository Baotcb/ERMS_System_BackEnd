using System;

namespace ERMS.Application.Features.Training.Queries.GetTrainingRequestDetail
{
    public sealed class TrainingRequestDetailDto
    {
        public Guid Id { get; set; }
        public Guid EnterpriseId { get; set; }
        public Guid? TrainingPlanId { get; set; }

        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = null!;

        public Guid RequestedById { get; set; }
        public string RequestedByName { get; set; } = null!;

        public string Subject { get; set; } = null!;
        public string Urgency { get; set; } = null!;
        public string Status { get; set; } = null!;

        public string? Description { get; set; }
        public string? TargetAudience { get; set; }
        public int? EstimatedParticipants { get; set; }
        public decimal? EstimatedBudget { get; set; }

        public string? ReviewNote { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}