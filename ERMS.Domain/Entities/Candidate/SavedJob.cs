using System;
using ERMS.Domain.Common;
using ERMS.Domain.Entities.Recruitment;

namespace ERMS.Domain.Entities.Candidate
{
    public class SavedJob : BaseEntity
    {
        public Guid CandidateId { get; set; }
        public Guid JobPostingId { get; set; }
        public DateTime SavedAt { get; set; } = DateTime.UtcNow;
        public string? Note { get; set; }

        public virtual Candidate Candidate { get; set; } = null!;
        public virtual JobPosting JobPosting { get; set; } = null!;
    }
}
