using ERMS.Domain.Common;
using System;

namespace ERMS.Domain.Entities
{
    public class Notification : BaseEntity
    {
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public string Title { get; set; } = string.Empty;
        public string? Message { get; set; }
        public string? Type { get; set; } // Info, Success, Warning
        public bool IsRead { get; set; } = false;
    }
}