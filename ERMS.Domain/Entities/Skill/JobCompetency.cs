using System;
using ERMS.Domain.Common;
using ERMS.Domain.Entities.Organization;

namespace ERMS.Domain.Entities.Skill
{
    public class JobCompetency : BaseEntity
    {
        public Guid JobPositionId { get; set; }
        public Guid SkillId { get; set; }
        public int RequiredLevel { get; set; } = 1;
        public string Importance { get; set; } = "Mandatory";

        public virtual JobPosition JobPosition { get; set; } = null!;
        public virtual Skill Skill { get; set; } = null!;
    }
}
