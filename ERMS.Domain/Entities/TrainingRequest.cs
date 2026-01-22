using ERMS.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace ERMS.Domain.Entities
{
    public class TrainingRequest
    {
        public Guid Id { get; set; }
        
        public Guid DepartmentId { get; set; }
        public Guid? TrainingPlanId { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string? Description { get; set; }
        
        [StringLength(1000)]
        public string? Justification { get; set; }
        
        public int EstimatedParticipants { get; set; }
        public decimal? EstimatedCost { get; set; }
        
        public DateTime RequestedStartDate { get; set; }
        public DateTime RequestedEndDate { get; set; }
        
        public TrainingRequestStatus Status { get; set; } = TrainingRequestStatus.Submitted;
        
        public Guid? ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
        
        [StringLength(1000)]
        public string? ReviewNotes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation Properties
        public virtual Department Department { get; set; } = null!;
        public virtual TrainingPlan? TrainingPlan { get; set; }
        public virtual User? Reviewer { get; set; }
    }
}