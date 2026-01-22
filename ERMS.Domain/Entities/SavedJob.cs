using ERMS.Domain.Common;

namespace ERMS.Domain.Entities
{
    public class SavedJob : BaseEntity
    {
        public Guid CandidateId { get; set; }
        public Guid JobPostingId { get; set; }
        
        public DateTime SavedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation Properties
        public virtual Candidate Candidate { get; set; } = null!;
        public virtual JobPosting JobPosting { get; set; } = null!;
    }
}