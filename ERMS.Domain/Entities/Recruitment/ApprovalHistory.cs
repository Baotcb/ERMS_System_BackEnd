using System;
using ERMS.Domain.Common;
using ERMS.Domain.Entities.Identity;

namespace ERMS.Domain.Entities.Recruitment
{
    public class ApprovalHistory : BaseEntity
    {
        public string EntityType { get; set; } = null!;
        public Guid EntityId { get; set; }
        public string Action { get; set; } = null!;
        public string? PreviousStatus { get; set; }
        public string NewStatus { get; set; } = null!;
        public Guid PerformedById { get; set; }
        public string? Note { get; set; }

        public virtual Identity.User PerformedBy { get; set; } = null!;
    }
}
