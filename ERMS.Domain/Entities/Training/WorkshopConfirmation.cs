using System;
using ERMS.Domain.Common;

namespace ERMS.Domain.Entities.Training
{
    public class WorkshopConfirmation : BaseEntity
    {
        public Guid CourseId { get; set; }
        public Guid ConfirmedByUserId { get; set; }
        public string EvidencePhotoUrls { get; set; } = "[]";
        public string? Notes { get; set; }
        public DateTime ConfirmedAt { get; set; } = DateTime.UtcNow;

        public virtual Course Course { get; set; } = null!;
    }
}
