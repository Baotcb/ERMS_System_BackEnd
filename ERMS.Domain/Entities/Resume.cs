using ERMS.Domain.Common;
using System;
using System.Collections.Generic;

namespace ERMS.Domain.Entities
{
    public class Resume : BaseEntity
    {
        public Guid CandidateId { get; set; }
        public Candidate Candidate { get; set; } = null!;

        public string? Title { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public bool IsPrimary { get; set; } = false;

        // Navigation properties
        public ICollection<Education> Educations { get; set; } = new List<Education>();
        public ICollection<WorkExperience> WorkExperiences { get; set; } = new List<WorkExperience>();
        public ICollection<Application> Applications { get; set; } = new List<Application>();
    }
}