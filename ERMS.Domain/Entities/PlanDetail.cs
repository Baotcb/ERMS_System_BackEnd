using ERMS.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace ERMS.Domain.Entities
{
    public class PlanDetail : BaseEntity
    {
        public Guid RecruitmentPlanId { get; set; }
        public int DepartmentId { get; set; } // ✅ int thay vì Guid
        public Guid RequestedById { get; set; }
        
        [Required]
        [StringLength(200)]
        public string PositionTitle { get; set; } = string.Empty;
        
        public int Quantity { get; set; } = 1;
        
        public string? JobDescription { get; set; } // NVARCHAR(MAX)
        public string? Requirements { get; set; } // NVARCHAR(MAX)
        
        public decimal? MinSalary { get; set; }
        public decimal? MaxSalary { get; set; }
        
        [StringLength(3)]
        public string Currency { get; set; } = "VND";
        
        [StringLength(20)]
        public string JobType { get; set; } = "Fulltime"; // Fulltime/Parttime/Contract/Internship
        
        [StringLength(20)]
        public string? ExperienceLevel { get; set; } // Intern/Fresher/Junior/Middle/Senior/Lead/Manager
        
        [StringLength(10)]
        public string Priority { get; set; } = "Normal"; // Low/Normal/High/Urgent
        
        public DateTime? ExpectedStartDate { get; set; }
        
        [StringLength(500)]
        public string? Reason { get; set; }
        
        [StringLength(20)]
        public string Status { get; set; } = "Pending"; // Pending/Approved/Rejected/Processing/Completed/Cancelled
        
        [StringLength(500)]
        public string? RejectionReason { get; set; }
        
        public Guid? ApprovedById { get; set; }
        public DateTime? ApprovedAt { get; set; }
        
        // Navigation Properties
        public virtual RecruitmentPlan RecruitmentPlan { get; set; } = null!;
        public virtual Department Department { get; set; } = null!;
        public virtual User RequestedBy { get; set; } = null!;
        public virtual User? ApprovedBy { get; set; }
        public virtual JobPosting? JobPosting { get; set; }
    }
}