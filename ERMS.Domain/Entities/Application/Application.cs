using System;
using System.Collections.Generic;
using ERMS.Domain.Common;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Organization;
using ERMS.Domain.Entities.Recruitment;

namespace ERMS.Domain.Entities.Application
{
    public class Application : BaseEntity
    {
        public Guid JobPostingId { get; set; }
        public Guid CandidateId { get; set; }
        public Guid? ResumeId { get; set; }
        public string? CoverLetter { get; set; }
        public decimal? ExpectedSalary { get; set; }
        public DateTime? AvailableStartDate { get; set; }
        public string Stage { get; set; } = "Applied";
        public DateTime? StageUpdatedAt { get; set; }
        public string Status { get; set; } = "Active";
        public string? Source { get; set; }
        public Guid? ReferredById { get; set; }
        public int? Rating { get; set; }
        public string? HRNote { get; set; }
        public string? RejectionReason { get; set; }
        public Guid? RejectedById { get; set; }
        public DateTime? RejectedAt { get; set; }
        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public virtual JobPosting JobPosting { get; set; } = null!;
        public virtual Candidate.Candidate Candidate { get; set; } = null!;
        public virtual Candidate.Resume? Resume { get; set; }
        public virtual Employee? ReferredBy { get; set; }
        public virtual Identity.User? RejectedBy { get; set; }
        
        public virtual CVScreeningResult? CVScreeningResult { get; set; }
        public virtual Offer? Offer { get; set; }
        public virtual ICollection<Interview> Interviews { get; set; } = new List<Interview>();
    }
}
