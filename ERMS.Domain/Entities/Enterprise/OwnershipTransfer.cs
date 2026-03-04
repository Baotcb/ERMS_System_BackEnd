using System;
using ERMS.Domain.Common;
using ERMS.Domain.Entities.Identity;

namespace ERMS.Domain.Entities.Enterprise
{
    public class OwnershipTransfer : BaseEntity
    {
        public Guid EnterpriseId { get; set; }
        public Guid FromUserId { get; set; }
        public Guid ToUserId { get; set; }
        public string? Reason { get; set; }
        public DateTime TransferredAt { get; set; } = DateTime.UtcNow;
        public Guid? ApprovedById { get; set; }
        public string? Note { get; set; }

        public virtual Enterprise Enterprise { get; set; } = null!;
        public virtual Identity.User FromUser { get; set; } = null!;
        public virtual Identity.User ToUser { get; set; } = null!;
        public virtual Identity.User? ApprovedBy { get; set; }
    }
}
