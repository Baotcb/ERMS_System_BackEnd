using ERMS.Domain.Common;
using ERMS.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace ERMS.Domain.Entities
{
    public class RecruitmentPlan : BaseEntity
    {
        public Guid EnterpriseId { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [StringLength(2000)]
        public string? Description { get; set; }
        
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        
        public int TotalPositions { get; set; } = 0;
        public decimal? TotalBudget { get; set; }
        
        [StringLength(20)]
        public string Status { get; set; } = "Draft";
        
        public Guid CreatedById { get; set; }
        public Guid? ApprovedById { get; set; }
        public DateTime? ApprovedAt { get; set; }
        
        // Navigation Properties
        public virtual Enterprise Enterprise { get; set; } = null!;
        public virtual User CreatedBy { get; set; } = null!;
        public virtual User? ApprovedBy { get; set; }
        public virtual ICollection<PlanDetail> PlanDetails { get; set; } = new HashSet<PlanDetail>();
        public virtual ICollection<JobPosting> JobPostings { get; set; } = new HashSet<JobPosting>();
    }
}