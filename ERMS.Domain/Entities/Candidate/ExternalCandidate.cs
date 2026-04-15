using System;
using System.Collections.Generic;
using ERMS.Domain.Common;

namespace ERMS.Domain.Entities.Candidate
{
    public class ExternalCandidate : BaseEntity
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? CurrentPosition { get; set; }
        public string Source { get; set; } = "HRImported";
        public Guid EnterpriseId { get; set; }
        public Guid CreatedById { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public virtual ICollection<Application.Application> Applications { get; set; } = new List<Application.Application>();
    }
}
