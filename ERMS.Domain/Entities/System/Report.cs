using System;
using ERMS.Domain.Common;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Constants.System;

namespace ERMS.Domain.Entities.System
{
    public class Report : BaseEntity
    {
        public Guid ReportedById { get; set; }
        public virtual User ReportedBy { get; set; } = null!;

        public string EntityType { get; set; } = null!;
        public Guid EntityId { get; set; }

        public string Reason { get; set; } = null!;
        public string? Description { get; set; }

        public string Status { get; set; } = ReportConstants.Status.Pending;

        public Guid? ResolvedById { get; set; }
        public virtual User? ResolvedBy { get; set; }

        public DateTime? ResolvedAt { get; set; }
        public string? AdminNote { get; set; }
        public string? ActionTaken { get; set; }
    }
}
