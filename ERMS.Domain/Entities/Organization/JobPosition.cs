using System;
using ERMS.Domain.Common;

namespace ERMS.Domain.Entities.Organization
{
    public class JobPosition : BaseEntity
    {
        public Guid EnterpriseId { get; set; }
        public string PositionName { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        // CreatedAt is in BaseEntity

        public virtual Enterprise.Enterprise Enterprise { get; set; } = null!;
    }
}
