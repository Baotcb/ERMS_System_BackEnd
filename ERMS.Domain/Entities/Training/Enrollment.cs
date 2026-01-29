using System;
using System.Collections.Generic;
using ERMS.Domain.Common;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Organization;

namespace ERMS.Domain.Entities.Training
{
    public class Enrollment : BaseEntity
    {
        public Guid CourseId { get; set; }
        public Guid EmployeeId { get; set; }
        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
        public Guid? EnrolledById { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Status { get; set; } = "NotStarted";
        public int Progress { get; set; }
        public DateTime? LastAccessedAt { get; set; }
        public string? CertificateUrl { get; set; }
        public DateTime? CertificateIssuedAt { get; set; }
        public string? Note { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public virtual Course Course { get; set; } = null!;
        public virtual Employee Employee { get; set; } = null!;
        public virtual Identity.User? EnrolledBy { get; set; }
        
        public virtual ICollection<LessonProgress> LessonProgresses { get; set; } = new List<LessonProgress>();
        public virtual ICollection<QuizAttempt> QuizAttempts { get; set; } = new List<QuizAttempt>();
    }
}
