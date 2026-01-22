using ERMS.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace ERMS.Domain.Entities
{
    public class Resume : BaseEntity
    {
        public Guid CandidateId { get; set; }
        
        [StringLength(200)]
        public string? Title { get; set; }
        
        [Required]
        [StringLength(200)]
        public string FileName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;
        
        [Required]
        [StringLength(10)]
        public string FileType { get; set; } = string.Empty; // pdf/doc/docx
        
        public bool IsPrimary { get; set; } = false;
        
        public string? ParsedData { get; set; } // JSON parsed data
        
        // Navigation Properties
        public virtual Candidate Candidate { get; set; } = null!;
        public virtual ICollection<Application> Applications { get; set; } = new HashSet<Application>();
    }
}