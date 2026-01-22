using ERMS.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace ERMS.Domain.Entities
{
    public class SubscriptionPlan : BaseEntity
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty; // Basic, Pro, Enterprise
        
        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty; // BASIC, PRO, ENTERPRISE
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        public decimal Price { get; set; } // Giá/tháng
        
        [StringLength(20)]
        public string BillingCycle { get; set; } = "Monthly"; // Monthly/Yearly
        
        public int? MaxEmployees { get; set; } // NULL = unlimited
        public int? MaxJobPostings { get; set; }
        public int? MaxCandidates { get; set; }
        
        public string? Features { get; set; } // JSON chứa features
        
        public bool IsActive { get; set; } = true;
        
        // Navigation Properties
        public virtual ICollection<Enterprise> Enterprises { get; set; } = new HashSet<Enterprise>();
        public virtual ICollection<SubscriptionHistory> SubscriptionHistories { get; set; } = new HashSet<SubscriptionHistory>();
    }
}