using System;
using System.Collections.Generic;
using ERMS.Domain.Common;

namespace ERMS.Domain.Entities.Training
{
    public class QuizAttempt : BaseEntity
    {
        public Guid EnrollmentId { get; set; }
        public Guid QuizId { get; set; }
        public int AttemptNumber { get; set; } = 1;
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public decimal? Score { get; set; }
        public bool? IsPassed { get; set; }
        public int TotalQuestions { get; set; }
        public int? CorrectAnswers { get; set; }
        public int? TimeTakenMinutes { get; set; }
        public string Status { get; set; } = "InProgress";

        public virtual Enrollment Enrollment { get; set; } = null!;
        public virtual Quiz Quiz { get; set; } = null!;
        public virtual ICollection<QuizAnswer> QuizAnswers { get; set; } = new List<QuizAnswer>();
    }
}
