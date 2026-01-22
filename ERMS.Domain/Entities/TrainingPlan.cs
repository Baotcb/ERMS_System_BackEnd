using ERMS.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace ERMS.Domain.Entities
{
    public class TrainingPlan
    {
        public Guid Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string? Description { get; set; }
        
        public int Year { get; set; }
        public int Quarter { get; set; }
        
        public TrainingPlanStatus Status { get; set; } = TrainingPlanStatus.Draft;
        
        public Guid? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation Properties
        public virtual User? Approver { get; set; }
        public virtual ICollection<TrainingRequest> TrainingRequests { get; set; } = new HashSet<TrainingRequest>();
        public virtual ICollection<Course> Courses { get; set; } = new HashSet<Course>();
    }
}