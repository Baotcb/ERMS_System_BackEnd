using ERMS.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace ERMS.Domain.Entities
{
    public class CandidateSkill : BaseEntity
    {
        public Guid CandidateId { get; set; }
        public Guid SkillId { get; set; }
        
        [StringLength(20)]
        public string? ProficiencyLevel { get; set; } // Beginner/Intermediate/Advanced/Expert
        
        public int? YearsOfExperience { get; set; }
        
        // Navigation Properties
        public virtual Candidate Candidate { get; set; } = null!;
        public virtual Skill Skill { get; set; } = null!;
    }
}