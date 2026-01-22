using ERMS.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace ERMS.Domain.Entities
{
    public class LearningProgress : BaseEntity
    {
        public Guid EnrollmentId { get; set; }
        public Guid? CourseSessionId { get; set; }
        public Guid EmployeeId { get; set; }
        
        [StringLength(100)]
        public string ActivityType { get; set; } = string.Empty; // Session, Material, Quiz, etc.
        
        public Guid? ActivityId { get; set; } // Reference to specific activity
        
        public bool IsCompleted { get; set; } = false;
        
        public decimal? ProgressPercentage { get; set; } = 0;
        
        public int? Score { get; set; }
        public int? MaxScore { get; set; }
        
        public TimeSpan? TimeSpent { get; set; }
        
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        
        [StringLength(1000)]
        public string? Notes { get; set; }
        
        // Navigation Properties
        public virtual Enrollment Enrollment { get; set; } = null!;
        public virtual CourseSession? CourseSession { get; set; }
        public virtual Employee Employee { get; set; } = null!;
    }
}