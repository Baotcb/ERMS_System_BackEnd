using System;

namespace ERMS.Domain.Entities
{
    public class JobSkill
    {
        public Guid JobId { get; set; }
        public JobPosting Job { get; set; } = null!;

        public int SkillId { get; set; }
        public Skill Skill { get; set; } = null!;

        public int Weight { get; set; } = 1; // 1-5
        public int MinProficiency { get; set; } = 1; // 1-5: Basic -> Expert
    }
}