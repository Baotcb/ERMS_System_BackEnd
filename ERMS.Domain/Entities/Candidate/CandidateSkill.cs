using System;
using ERMS.Domain.Common;

namespace ERMS.Domain.Entities.Candidate
{
    public class CandidateSkill : BaseEntity
    {
        public Guid CandidateId { get; set; }
        public Guid SkillId { get; set; }
        public int? ProficiencyLevel { get; set; }
        public int? YearsOfExperience { get; set; }

        public virtual Candidate Candidate { get; set; } = null!;
        public virtual Skill.Skill Skill { get; set; } = null!;
    }
}
