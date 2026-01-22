using ERMS.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace ERMS.Domain.Entities
{
    public class Offer : BaseEntity
    {
        public Guid ApplicationId { get; set; }
        
        [Required]
        [StringLength(200)]
        public string PositionTitle { get; set; } = string.Empty;
        
        public int DepartmentId { get; set; } // ✅ int thay vì Guid
        
        public decimal OfferedSalary { get; set; }
        
        [StringLength(500)]
        public string? Bonus { get; set; }
        
        [StringLength(1000)]
        public string? Benefits { get; set; }
        
        [StringLength(20)]
        public string JobType { get; set; } = "Fulltime";
        
        public DateTime StartDate { get; set; }
        public DateTime ExpiresAt { get; set; }
        
        [StringLength(500)]
        public string? OfferLetterUrl { get; set; }
        
        [StringLength(20)]
        public string Status { get; set; } = "Draft"; // Draft/PendingApproval/Approved/Sent/Accepted/Rejected/Expired/Negotiating/Withdrawn
        
        [StringLength(20)]
        public string? CandidateResponse { get; set; } // Accepted/Rejected/Negotiating
        
        public DateTime? CandidateRespondedAt { get; set; }
        
        [StringLength(1000)]
        public string? NegotiationNotes { get; set; }
        
        [StringLength(500)]
        public string? RejectionReason { get; set; }
        
        public Guid CreatedById { get; set; }
        public Guid? ApprovedById { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? SentAt { get; set; }
        
        // Navigation Properties
        public virtual Application Application { get; set; } = null!;
        public virtual Department Department { get; set; } = null!;
        public virtual User CreatedBy { get; set; } = null!;
        public virtual User? ApprovedBy { get; set; }
    }
}