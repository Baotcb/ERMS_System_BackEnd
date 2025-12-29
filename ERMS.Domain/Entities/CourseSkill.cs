using System;

namespace ERMS.Domain.Entities
{
    public class CourseSkill
    {
        public Guid CourseId { get; set; }
        public Course Course { get; set; } = null!;

        public int SkillId { get; set; }
        public Skill Skill { get; set; } = null!;

        public int TargetProficiency { get; set; } = 1; // 1-5
    }
}