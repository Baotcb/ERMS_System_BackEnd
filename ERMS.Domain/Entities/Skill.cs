using ERMS.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace ERMS.Domain.Entities
{
    public class Skill : BaseEntity
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(50)]
        public string? Category { get; set; } // Technical/Soft/Language/...
        
        public bool IsVerified { get; set; } = false;
        
        // Navigation Properties
        public virtual ICollection<JobSkill> JobSkills { get; set; } = new HashSet<JobSkill>();
        public virtual ICollection<CandidateSkill> CandidateSkills { get; set; } = new HashSet<CandidateSkill>();
    }
}