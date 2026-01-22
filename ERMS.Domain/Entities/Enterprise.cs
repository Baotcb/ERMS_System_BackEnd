using ERMS.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace ERMS.Domain.Entities
{
    public class Enterprise : BaseEntity
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty; // For subdomain
        
        [StringLength(1000)]
        public string? Description { get; set; }
        
        [StringLength(500)]
        public string? LogoUrl { get; set; }
        
        [StringLength(200)]
        public string? Website { get; set; }
        
        [StringLength(100)]
        [EmailAddress]
        public string? Email { get; set; }
        
        [StringLength(20)]
        public string? Phone { get; set; }
        
        [StringLength(500)]
        public string? Address { get; set; }
        
        [StringLength(20)]
        public string? TaxCode { get; set; }
        
        [StringLength(100)]
        public string? Industry { get; set; }
        
        [StringLength(20)]
        public string? CompanySize { get; set; } // 1-50, 51-200, ...
        
        [StringLength(20)]
        public string Status { get; set; } = "Active"; // Active/Suspended/Inactive
        
        public Guid? CurrentSubscriptionId { get; set; }
        public DateTime? SubscriptionExpiresAt { get; set; }
        
        // Navigation Properties
        public virtual SubscriptionPlan? CurrentSubscription { get; set; }
        public virtual ICollection<User> Users { get; set; } = new HashSet<User>();
        public virtual ICollection<Department> Departments { get; set; } = new HashSet<Department>();
        public virtual ICollection<RecruitmentPlan> RecruitmentPlans { get; set; } = new HashSet<RecruitmentPlan>();
        public virtual ICollection<JobPosting> JobPostings { get; set; } = new HashSet<JobPosting>();
        public virtual ICollection<SubscriptionHistory> SubscriptionHistories { get; set; } = new HashSet<SubscriptionHistory>();
    }
}