using System;
using System.Collections.Generic;
using ERMS.Domain.Common;

namespace ERMS.Domain.Entities.Training
{
    public class WorkshopConfirmation : BaseEntity
    {
        public Guid CourseId { get; set; }
        public Guid ConfirmedByUserId { get; set; }
        public List<string> EvidencePhotoUrls { get; set; } = new();
        public string? Notes { get; set; }
        public DateTime ConfirmedAt { get; set; } = DateTime.UtcNow;

        public virtual Course Course { get; set; } = null!;
    }
}
