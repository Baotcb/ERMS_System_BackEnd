using System;
using System.Collections.Generic;
using ERMS.Domain.Common;

namespace ERMS.Domain.Entities.Skill
{
    public class Skill : BaseEntity
    {
        public Guid? EnterpriseId { get; set; }
        public string SkillName { get; set; } = null!;
        public string? SkillCategory { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public virtual Enterprise.Enterprise? Enterprise { get; set; }
    }
}
