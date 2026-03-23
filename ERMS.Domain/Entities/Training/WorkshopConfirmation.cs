using ERMS.Domain.Common;
using ERMS.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.Domain.Entities.Training
{
    public class WorkshopConfirmation : BaseEntityInt
    {
        public Guid CourseId { get; set; }
        public Guid ConfirmedByUserId { get; set; }
        public string EvidencePhotoUrls { get; set; } = "[]";
        public string? Notes { get; set; }
        public DateTime ConfirmedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public virtual Course Course { get; set; } = null!;
        public virtual User ConfirmedByUser { get; set; } = null!;
    }
}
