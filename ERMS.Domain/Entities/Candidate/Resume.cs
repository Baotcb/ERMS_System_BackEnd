using System;
using ERMS.Domain.Common;

namespace ERMS.Domain.Entities.Candidate
{
    public class Resume : BaseEntity
    {
        public Guid CandidateId { get; set; }
        public string FileName { get; set; } = null!;
        public string FileUrl { get; set; } = null!;
        public int? FileSize { get; set; }
        public string? FileType { get; set; }
        public bool IsDefault { get; set; }
        public string? ParsedData { get; set; } // JSON
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public virtual Candidate Candidate { get; set; } = null!;
    }
}
