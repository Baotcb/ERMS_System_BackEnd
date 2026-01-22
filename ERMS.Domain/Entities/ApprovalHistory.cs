using ERMS.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace ERMS.Domain.Entities
{
    public class ApprovalHistory : BaseEntity
    {
        [Required]
        [StringLength(50)]
        public string EntityType { get; set; } = string.Empty; // PlanDetail/Offer/RecruitmentPlan
        
        public Guid EntityId { get; set; }
        public Guid ApproverId { get; set; }
        
        [Required]
        [StringLength(20)]
        public string Action { get; set; } = string.Empty; // Approved/Rejected/Returned
        
        [StringLength(500)]
        public string? Comment { get; set; }
        
        [StringLength(20)]
        public string? PreviousStatus { get; set; }
        
        [StringLength(20)]
        public string NewStatus { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation Properties
        public virtual User Approver { get; set; } = null!;
    }
}