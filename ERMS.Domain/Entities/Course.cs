using ERMS.Domain.Common;
using ERMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ERMS.Domain.Entities
{
    public class Course : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(100)]
        public string? Category { get; set; }

        public int DurationHours { get; set; }

        public int MaxParticipants { get; set; }

        public CourseStatus Status { get; set; } = CourseStatus.Draft;

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public bool IsPublic { get; set; } = true;
        public bool RequiresApproval { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual TrainingPlan? TrainingPlan { get; set; }
        public virtual Employee Creator { get; set; } = null!;
        public virtual ICollection<CourseSession> CourseSessions { get; set; } = new HashSet<CourseSession>();
        public virtual ICollection<CourseMaterial> CourseMaterials { get; set; } = new HashSet<CourseMaterial>();
        public virtual ICollection<Enrollment> Enrollments { get; set; } = new HashSet<Enrollment>();
        public virtual ICollection<Certificate> Certificates { get; set; } = new HashSet<Certificate>();
        public virtual ICollection<CourseFeedback> CourseFeedbacks { get; set; } = new HashSet<CourseFeedback>();
        public virtual ICollection<Skill> Skills { get; set; } = new HashSet<Skill>();
    }
}