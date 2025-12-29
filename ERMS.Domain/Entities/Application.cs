using ERMS.Domain.Common;
using System;
using System.Collections.Generic;

namespace ERMS.Domain.Entities
{
    public class Application : BaseEntity
    {
        public Guid JobId { get; set; }
        public JobPosting Job { get; set; } = null!;

        public Guid CandidateId { get; set; }
        public Candidate Candidate { get; set; } = null!;

        public Guid ResumeId { get; set; }
        public Resume Resume { get; set; } = null!;

        public string CvUrl { get; set; } = string.Empty;
        public string? CoverLetter { get; set; }
        public double? MatchingScore { get; set; }
        public string? Category { get; set; }
        public string ApplicantType { get; set; } = "External"; // Internal, External
        public string Status { get; set; } = "Applied";
        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
        public DateTime? WithdrawnAt { get; set; }
        public string? WithdrawReason { get; set; }

        // Navigation properties
        public ICollection<Interview> Interviews { get; set; } = new List<Interview>();
        public Offer? Offer { get; set; }
    }
}