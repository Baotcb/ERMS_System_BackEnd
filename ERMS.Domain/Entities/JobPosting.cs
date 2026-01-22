using ERMS.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace ERMS.Domain.Entities
{
    public class JobPosting : BaseEntity
    {
        public Guid EnterpriseId { get; set; }
        public Guid? PlanDetailId { get; set; }
        public int? DepartmentId { get; set; } // ✅ int? thay vì Guid?
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [StringLength(250)]
        public string Slug { get; set; } = string.Empty; // URL-friendly
        
        public string? Description { get; set; } // NVARCHAR(MAX)
        public string? Requirements { get; set; } // NVARCHAR(MAX)
        public string? Benefits { get; set; } // NVARCHAR(MAX)
        
        public decimal? MinSalary { get; set; }
        public decimal? MaxSalary { get; set; }
        
        [StringLength(3)]
        public string Currency { get; set; } = "VND";
        
        public bool IsSalaryVisible { get; set; } = true;
        
        [StringLength(200)]
        public string? Location { get; set; }
        
        [StringLength(20)]
        public string JobType { get; set; } = "Fulltime"; // Fulltime/Parttime/Contract/Internship
        
        [StringLength(20)]
        public string? ExperienceLevel { get; set; }
        
        [StringLength(20)]
        public string PostingType { get; set; } = "External"; // Internal/External/Both
        
        [StringLength(20)]
        public string Status { get; set; } = "Draft"; // Draft/Published/Paused/Closed/Filled
        
        public int ViewCount { get; set; } = 0;
        public int ApplicationCount { get; set; } = 0;
        
        public DateTime? PublishedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        
        public Guid CreatedById { get; set; }
        
        // Navigation Properties
        public virtual Enterprise Enterprise { get; set; } = null!;
        public virtual PlanDetail? PlanDetail { get; set; }
        public virtual Department? Department { get; set; }
        public virtual User CreatedBy { get; set; } = null!;
        public virtual ICollection<Application> Applications { get; set; } = new HashSet<Application>();
        public virtual ICollection<JobSkill> JobSkills { get; set; } = new HashSet<JobSkill>();
        public virtual ICollection<SavedJob> SavedJobs { get; set; } = new HashSet<SavedJob>();
    }
}