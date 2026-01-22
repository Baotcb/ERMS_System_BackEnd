using ERMS.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace ERMS.Domain.Entities
{   
    public class Notification : BaseEntity
    {
        public Guid UserId { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [StringLength(1000)]
        public string Content { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string Type { get; set; } = string.Empty;
        
        [StringLength(50)]
        public string? ReferenceType { get; set; }
        
        public Guid? ReferenceId { get; set; }
        
        [StringLength(500)]
        public string? ActionUrl { get; set; }
        
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation Properties
        public virtual User User { get; set; } = null!;
    }
}