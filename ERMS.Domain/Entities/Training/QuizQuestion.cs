using System;
using ERMS.Domain.Common;

namespace ERMS.Domain.Entities.Training
{
    public class QuizQuestion : BaseEntity
    {
        public Guid QuizId { get; set; }
        public string QuestionText { get; set; } = null!;
        public string QuestionType { get; set; } = "MultipleChoice";
        public string Options { get; set; } = null!; // JSON
        public string CorrectAnswer { get; set; } = null!;
        public string? Explanation { get; set; }
        public int Points { get; set; } = 1;
        public int OrderIndex { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; } = true;

        public virtual Quiz Quiz { get; set; } = null!;
    }
}
