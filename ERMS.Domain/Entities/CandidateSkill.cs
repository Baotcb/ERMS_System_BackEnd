using System;

namespace ERMS.Domain.Entities
{
    public class CandidateSkill
    {
        public Guid CandidateId { get; set; }
        public Candidate Candidate { get; set; } = null!;

        public int SkillId { get; set; }
        public Skill Skill { get; set; } = null!;

        public bool IsVerified { get; set; } = false;
        public int Proficiency { get; set; } = 1; // 1-5
    }
}