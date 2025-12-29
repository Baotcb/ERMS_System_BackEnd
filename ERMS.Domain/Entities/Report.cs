using ERMS.Domain.Common;
using System;

namespace ERMS.Domain.Entities
{
    public class Report : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string? Type { get; set; } // Recruitment, Training

        public Guid GeneratedBy { get; set; }
        public Employee Generator { get; set; } = null!;

        public string? FileUrl { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}