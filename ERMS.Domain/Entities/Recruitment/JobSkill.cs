using System;
using ERMS.Domain.Common;

namespace ERMS.Domain.Entities.Recruitment
{
    public class JobSkill : BaseEntity
    {
        public Guid JobPostingId { get; set; }
        public Guid SkillId { get; set; }
        public bool IsRequired { get; set; } = true;
        public int? MinLevel { get; set; }

        public virtual JobPosting JobPosting { get; set; } = null!;
        public virtual Skill.Skill Skill { get; set; } = null!;
    }
}
