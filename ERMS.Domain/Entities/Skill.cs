using ERMS.Domain.Common;
using System.Collections.Generic;

namespace ERMS.Domain.Entities
{
    public class Skill : BaseEntityInt
    {
        public Guid? EnterpriseId { get; set; } // null = global skill
        public Enterprise? Enterprise { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // Technical, SoftSkill
        
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public ICollection<JobSkill> JobSkills { get; set; } = new List<JobSkill>();
        public ICollection<CandidateSkill> CandidateSkills { get; set; } = new List<CandidateSkill>();
        public ICollection<CourseSkill> CourseSkills { get; set; } = new List<CourseSkill>();
    }
}