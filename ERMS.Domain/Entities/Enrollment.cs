using ERMS.Domain.Common;
using ERMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ERMS.Domain.Entities
{
    public class Enrollment : BaseEntity
    {
        public Guid CourseId { get; set; }
        public Course Course { get; set; } = null!;

        public Guid EmployeeId { get; set; }
        public Employee Employee { get; set; } = null!;

        public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Enrolled;

        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        public decimal? ProgressPercentage { get; set; } = 0;

        public int? FinalScore { get; set; }
        public bool? Passed { get; set; }

        [StringLength(1000)]
        public string? CompletionNotes { get; set; }

        // Navigation Properties
        public virtual ICollection<LearningProgress> LearningProgresses { get; set; } = new HashSet<LearningProgress>();
    }
}