using System;
using System.Collections.Generic;
using ERMS.Domain.Common;

namespace ERMS.Domain.Entities.Training
{
    public class Quiz : BaseEntity
    {
        public Guid CourseId { get; set; }
        public string QuizTitle { get; set; } = null!;
        public string? Description { get; set; }
        public int? TimeLimitMinutes { get; set; }
        public int PassingScore { get; set; } = 80;
        public int? MaxAttempts { get; set; }
        public bool ShuffleQuestions { get; set; } = true;
        public bool ShuffleAnswers { get; set; } = true;
        public bool ShowCorrectAnswers { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public virtual Course Course { get; set; } = null!;
        public virtual ICollection<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();
    }
}
