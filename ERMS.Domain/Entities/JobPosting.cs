using ERMS.Domain.Common;
using System;
using System.Collections.Generic;

namespace ERMS.Domain.Entities
{
    public class JobPosting : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Requirements { get; set; }
        public decimal? MinSalary { get; set; }
        public decimal? MaxSalary { get; set; }
        public string Currency { get; set; } = "VND";
        public string? Location { get; set; }

        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }

        public Guid CreatorId { get; set; }
        public Employee Creator { get; set; } = null!;

        public string PostingType { get; set; } = "External"; // Internal, External, Mixed
        public string Status { get; set; } = "Draft"; // Draft, PendingApproval, Open, Closed
        public DateTime? PublishDate { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int ViewCount { get; set; } = 0;

        // Navigation properties
        public ICollection<JobSkill> JobSkills { get; set; } = new List<JobSkill>();
        public ICollection<Application> Applications { get; set; } = new List<Application>();
    }
}