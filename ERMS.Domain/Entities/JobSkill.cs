using ERMS.Domain.Common;
using System.ComponentModel.DataAnnotations;
using System;

namespace ERMS.Domain.Entities
{
    public class JobSkill : BaseEntity
    {
        public Guid JobPostingId { get; set; }
        public Guid SkillId { get; set; }
        
        public bool IsRequired { get; set; } = true;
        public int? MinYearsExperience { get; set; }
        
        // Navigation Properties
        public virtual JobPosting JobPosting { get; set; } = null!;
        public virtual Skill Skill { get; set; } = null!;
    }
}