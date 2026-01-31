using System;
using ERMS.Domain.Common;

namespace ERMS.Domain.Entities.Training
{
    public class LessonProgress : BaseEntity
    {
        public Guid EnrollmentId { get; set; }
        public Guid LessonId { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int WatchPercentage { get; set; }
        public int? LastPosition { get; set; }
        public int TimeSpentMinutes { get; set; }
        public string Status { get; set; } = "NotStarted";

        public virtual Enrollment Enrollment { get; set; } = null!;
        public virtual Lesson Lesson { get; set; } = null!;
    }
}
