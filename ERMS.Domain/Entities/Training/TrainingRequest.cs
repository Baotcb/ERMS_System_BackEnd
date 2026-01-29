using System;
using ERMS.Domain.Common;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Organization;

namespace ERMS.Domain.Entities.Training
{
    public class TrainingRequest : BaseEntity
    {
        public Guid EnterpriseId { get; set; }
        public Guid? TrainingPlanId { get; set; }
        public int DepartmentId { get; set; }
        public Guid RequestedById { get; set; }
        public string Subject { get; set; } = null!;
        public string Urgency { get; set; } = "Normal";
        public string? Description { get; set; }
        public string? TargetAudience { get; set; }
        public int? EstimatedParticipants { get; set; }
        public decimal? EstimatedBudget { get; set; }
        public string Status { get; set; } = "Pending";
        public string? ReviewNote { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public virtual Enterprise.Enterprise Enterprise { get; set; } = null!;
        public virtual TrainingPlan? TrainingPlan { get; set; }
        public virtual Department Department { get; set; } = null!;
        public virtual Identity.User RequestedBy { get; set; } = null!;
    }
}
