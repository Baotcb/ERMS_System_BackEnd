using ERMS.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace ERMS.Domain.Entities
{
    public class CVScreeningResult : BaseEntity
    {
        public Guid ApplicationId { get; set; }
        
        public decimal OverallScore { get; set; } // 0-100
        public decimal? SkillMatchScore { get; set; }
        public decimal? ExperienceMatchScore { get; set; }
        public decimal? EducationMatchScore { get; set; }
        
        public string? SkillMatchDetails { get; set; } // JSON
        public string? ExperienceMatchDetails { get; set; } // JSON
        
        [StringLength(2000)]
        public string? AISummary { get; set; }
        
        [StringLength(1000)]
        public string? Strengths { get; set; }
        
        [StringLength(1000)]
        public string? Weaknesses { get; set; }
        
        [Required]
        [StringLength(30)]
        public string Recommendation { get; set; } = string.Empty; // HighlyRecommended/Recommended/MaybeRecommended/NotRecommended
        
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation Properties
        public virtual Application Application { get; set; } = null!;
    }
}