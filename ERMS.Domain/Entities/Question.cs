using ERMS.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace ERMS.Domain.Entities
{
    public class Question : BaseEntity
    {
        public Guid CourseId { get; set; }
        
        [Required]
        public string QuestionText { get; set; } = string.Empty;
        
        [StringLength(50)]
        public string QuestionType { get; set; } = "MultipleChoice"; // MultipleChoice, TrueFalse, Essay, etc.
        
        public int Points { get; set; } = 1;
        
        public bool IsRequired { get; set; } = true;
        
        public int Order { get; set; }
        
        [StringLength(1000)]
        public string? Explanation { get; set; }
        
        // Navigation Properties
        public virtual Course Course { get; set; } = null!;
        public virtual ICollection<AnswerOption> AnswerOptions { get; set; } = new HashSet<AnswerOption>();
    }
}