using System;
using ERMS.Domain.Common;

namespace ERMS.Domain.Entities.Candidate
{
    public class Education : BaseEntity
    {
        public Guid CandidateId { get; set; }
        public string Institution { get; set; } = null!;
        public string Degree { get; set; } = null!;
        public string? FieldOfStudy { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsCurrent { get; set; }
        public string? Grade { get; set; }
        public string? Description { get; set; }

        public virtual Candidate Candidate { get; set; } = null!;
    }
}
