using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ERMS.Domain.Common;

namespace ERMS.Domain.Entities
{
    public class Candidate : BaseEntity
    {
        public Guid UserId { get; set; }
        
        [StringLength(2000)]
        public string? Summary { get; set; }
        
        public decimal? ExpectedSalary { get; set; }
        public int? YearsOfExperience { get; set; }
        
        [StringLength(50)]
        public string? HighestEducation { get; set; }
        
        [StringLength(30)]
        public string JobSearchStatus { get; set; } = "OpenToOffers"; // ActivelyLooking/OpenToOffers/NotLooking
        
        [StringLength(500)]
        public string? PreferredLocations { get; set; } // JSON array
        
        [StringLength(200)]
        public string? PreferredJobTypes { get; set; } // JSON
        
        public bool IsProfilePublic { get; set; } = true;
        public int ProfileCompleteness { get; set; } = 0;
        
        // Navigation Properties
        public virtual User User { get; set; } = null!;
        public virtual ICollection<Education> Educations { get; set; } = new HashSet<Education>();
        public virtual ICollection<WorkExperience> WorkExperiences { get; set; } = new HashSet<WorkExperience>();
        public virtual ICollection<Resume> Resumes { get; set; } = new HashSet<Resume>();
        public virtual ICollection<Application> Applications { get; set; } = new HashSet<Application>();
        public virtual ICollection<CandidateSkill> CandidateSkills { get; set; } = new HashSet<CandidateSkill>();
        public virtual ICollection<SavedJob> SavedJobs { get; set; } = new HashSet<SavedJob>();
    }
}