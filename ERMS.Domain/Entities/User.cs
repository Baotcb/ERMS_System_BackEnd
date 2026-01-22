using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ERMS.Domain.Entities
{
    public class User : IdentityUser<Guid>
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? AvatarUrl { get; set; }
        
        public DateTime? DateOfBirth { get; set; }
        
        [StringLength(10)]
        public string? Gender { get; set; }
        
        [StringLength(500)]
        public string? Address { get; set; }
        
        public Guid? EnterpriseId { get; set; }
        public int? DepartmentId { get; set; } // ✅ Đổi từ Guid? thành int?
        
        public DateTime? LastLoginAt { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation Properties
        public virtual Enterprise? Enterprise { get; set; }
        public virtual Department? Department { get; set; }
        public virtual Employee? Employee { get; set; }
        public virtual Candidate? Candidate { get; set; }
        public virtual ICollection<Notification> Notifications { get; set; } = new HashSet<Notification>();
        public virtual ICollection<Education> Educations { get; set; } = new HashSet<Education>();
        public virtual ICollection<WorkExperience> WorkExperiences { get; set; } = new HashSet<WorkExperience>();
    }
}
