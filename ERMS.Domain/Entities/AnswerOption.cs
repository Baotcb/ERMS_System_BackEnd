using ERMS.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace ERMS.Domain.Entities
{
    public class AnswerOption : BaseEntity
    {
        public Guid QuestionId { get; set; }
        
        [Required]
        [StringLength(1000)]
        public string OptionText { get; set; } = string.Empty;
        
        public bool IsCorrect { get; set; } = false;
        
        public int Order { get; set; }
        
        [StringLength(1000)]
        public string? Explanation { get; set; }
        
        // Navigation Properties
        public virtual Question Question { get; set; } = null!;
    }
}