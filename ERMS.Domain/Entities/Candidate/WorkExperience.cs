using System;
using ERMS.Domain.Common;

namespace ERMS.Domain.Entities.Candidate
{
    public class WorkExperience : BaseEntity
    {
        public Guid CandidateId { get; set; }
        public string CompanyName { get; set; } = null!;
        public string Position { get; set; } = null!;
        public string? Location { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsCurrent { get; set; }
        public string? Description { get; set; }
        public string? EmploymentType { get; set; }

        public virtual Candidate Candidate { get; set; } = null!;
    }
}
