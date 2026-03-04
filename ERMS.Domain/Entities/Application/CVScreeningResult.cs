using System;
using ERMS.Domain.Common;

namespace ERMS.Domain.Entities.Application
{
    public class CVScreeningResult : BaseEntity
    {
        public Guid ApplicationId { get; set; }
        public decimal OverallScore { get; set; }
        public decimal? SkillMatchScore { get; set; }
        public decimal? ExperienceMatchScore { get; set; }
        public decimal? EducationMatchScore { get; set; }
        public decimal? KeywordMatchScore { get; set; }
        public string? MatchedSkills { get; set; } // JSON
        public string? MissingSkills { get; set; } // JSON
        public string? Strengths { get; set; } // JSON
        public string? Concerns { get; set; } // JSON
        public string? Summary { get; set; }
        public string? RawResponse { get; set; }
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
        public string? AIModel { get; set; }

        public virtual Application Application { get; set; } = null!;
    }
}
