using ERMS.Domain.Common;
using System;
using System.ComponentModel.DataAnnotations;

namespace ERMS.Domain.Entities
{
    public class CourseFeedback : BaseEntity
    {
        public Guid CourseId { get; set; }
        public Guid EmployeeId { get; set; }
        
        public int Rating { get; set; } // 1-5 stars
        
        [StringLength(1000)]
        public string? Comment { get; set; }
        
        [StringLength(1000)]
        public string? Suggestions { get; set; }
        
        public int? ContentRating { get; set; }
        public int? InstructorRating { get; set; }
        public int? MaterialRating { get; set; }
        
        public bool WouldRecommend { get; set; } = true;
        
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation Properties
        public virtual Course Course { get; set; } = null!;
        public virtual Employee Employee { get; set; } = null!;
    }
}