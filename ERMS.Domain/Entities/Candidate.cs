using System;
using System.Collections.Generic;

namespace ERMS.Domain.Entities
{
    public class Candidate
    {
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public string? AboutMe { get; set; }

        // Navigation properties
        public ICollection<Resume> Resumes { get; set; } = new List<Resume>();
        public ICollection<Application> Applications { get; set; } = new List<Application>();
        public ICollection<CandidateSkill> CandidateSkills { get; set; } = new List<CandidateSkill>();
    }
}