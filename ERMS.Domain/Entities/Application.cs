using ERMS.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace ERMS.Domain.Entities
{
    public class Application : BaseEntity
    {
        public Guid JobPostingId { get; set; }
        public Guid CandidateId { get; set; }
        public Guid? ResumeId { get; set; }
        
        public string? CoverLetter { get; set; } // NVARCHAR(MAX)
        
        [StringLength(50)]
        public string? Source { get; set; } // LinkedIn/Website/Referral
        
        public Guid? ReferredById { get; set; }
        
        [Required]
        [StringLength(30)]
        public string Stage { get; set; } = "New"; // New/AIScreening/HRReview/DeptReview/Interview/Offer/Hired/Rejected
        
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending"; // Pending/InProgress/Passed/Failed/Withdrawn/OnHold
        
        public DateTime? WithdrawnAt { get; set; }
        
        [StringLength(500)]
        public string? WithdrawReason { get; set; }
        
        [StringLength(1000)]
        public string? Notes { get; set; }
        
        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation Properties
        public virtual JobPosting JobPosting { get; set; } = null!;
        public virtual Candidate Candidate { get; set; } = null!;
        public virtual Resume? Resume { get; set; }
        public virtual User? ReferredBy { get; set; }
        public virtual CVScreeningResult? CVScreeningResult { get; set; }
        public virtual ICollection<Interview> Interviews { get; set; } = new HashSet<Interview>();
        public virtual Offer? Offer { get; set; }
    }
}