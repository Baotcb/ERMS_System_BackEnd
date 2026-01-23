using System;
using ERMS.Domain.Common;

namespace ERMS.Domain.Entities.Training
{
    public class Lesson : BaseEntity
    {
        public Guid CourseId { get; set; }
        public string LessonTitle { get; set; } = null!;
        public string? Description { get; set; }
        public int OrderIndex { get; set; }
        public string ContentType { get; set; } = "Video";
        public string? VideoUrl { get; set; }
        public int? VideoDurationMinutes { get; set; }
        public string? DocumentUrl { get; set; }
        public string? ExternalLinkUrl { get; set; }
        public string? Content { get; set; }
        public bool IsPreview { get; set; }
        public int? EstimatedMinutes { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public virtual Course Course { get; set; } = null!;
    }
}
