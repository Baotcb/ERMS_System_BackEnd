using System.ComponentModel.DataAnnotations;

namespace ERMS.Domain.Entities
{
    public class CourseMaterial
    {
        public Guid Id { get; set; }
        
        public Guid CourseId { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string? Description { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Type { get; set; } = string.Empty; // Video, PDF, Slide, Quiz, etc.
        
        [StringLength(500)]
        public string? FilePath { get; set; }
        
        [StringLength(500)]
        public string? ExternalUrl { get; set; }
        
        public long? FileSize { get; set; }
        
        public int Order { get; set; }
        
        public bool IsRequired { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation Properties
        public virtual Course Course { get; set; } = null!;
    }
}