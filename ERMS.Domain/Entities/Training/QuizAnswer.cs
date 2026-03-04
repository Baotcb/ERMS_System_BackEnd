using System;
using ERMS.Domain.Common;

namespace ERMS.Domain.Entities.Training
{
    public class QuizAnswer : BaseEntity
    {
        public Guid QuizAttemptId { get; set; }
        public Guid QuizQuestionId { get; set; }
        public string? SelectedAnswer { get; set; }
        public bool? IsCorrect { get; set; }
        public int PointsEarned { get; set; }
        public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;

        public virtual QuizAttempt QuizAttempt { get; set; } = null!;
        public virtual QuizQuestion QuizQuestion { get; set; } = null!;
    }
}
