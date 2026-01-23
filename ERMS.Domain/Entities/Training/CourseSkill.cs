using System;
using ERMS.Domain.Common;

namespace ERMS.Domain.Entities.Training
{
    public class CourseSkill : BaseEntity
    {
        public Guid CourseId { get; set; }
        public Guid SkillId { get; set; }
        public int? SkillLevelGained { get; set; }

        public virtual Course Course { get; set; } = null!;
        public virtual Skill.Skill Skill { get; set; } = null!;
    }
}
