using System;
using ERMS.Domain.Common;
using ERMS.Domain.Entities.Identity;

namespace ERMS.Domain.Entities.System
{
    public class Notification : BaseEntity
    {
        public Guid UserId { get; set; }
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string NotificationType { get; set; } = null!;
        public string? EntityType { get; set; }
        public Guid? EntityId { get; set; }
        public string? ActionUrl { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public bool IsSent { get; set; }
        public DateTime? SentAt { get; set; }

        public virtual Identity.User User { get; set; } = null!;
    }
}
