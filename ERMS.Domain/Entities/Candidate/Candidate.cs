using System;
using System.Collections.Generic;
using ERMS.Domain.Common;
using ERMS.Domain.Entities.Identity;

namespace ERMS.Domain.Entities.Candidate
{
    public class Candidate : BaseEntity
    {
        public Guid UserId { get; set; }
        public string? AboutMe { get; set; }
        public string? Headline { get; set; }
        public string? CurrentPosition { get; set; }
        public string? CurrentCompany { get; set; }
        public string? Location { get; set; }
        public string? LinkedInUrl { get; set; }
        public string? PortfolioUrl { get; set; }
        public decimal? ExpectedSalary { get; set; }
        public int? NoticePeriod { get; set; }
        public bool IsOpenToWork { get; set; } = true;
        public string? PreferredJobTypes { get; set; } // JSON
        public string? PreferredLocations { get; set; } // JSON
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public virtual Identity.User User { get; set; } = null!;
        public virtual ICollection<Education> Educations { get; set; } = new List<Education>();
        public virtual ICollection<WorkExperience> WorkExperiences { get; set; } = new List<WorkExperience>();
        public virtual ICollection<CandidateSkill> CandidateSkills { get; set; } = new List<CandidateSkill>();
        public virtual ICollection<Resume> Resumes { get; set; } = new List<Resume>();
        public virtual ICollection<SavedJob> SavedJobs { get; set; } = new List<SavedJob>();
        public virtual ICollection<Application.Application> Applications { get; set; } = new List<Application.Application>();
    }
}
