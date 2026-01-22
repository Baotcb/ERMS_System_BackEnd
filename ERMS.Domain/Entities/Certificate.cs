using ERMS.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace ERMS.Domain.Entities
{
    public class Certificate : BaseEntity
    {
        public Guid CourseId { get; set; }
        public Guid EmployeeId { get; set; }
        
        [Required]
        [StringLength(200)]
        public string CertificateName { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string CertificateNumber { get; set; } = string.Empty;
        
        public DateTime IssuedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ExpirationDate { get; set; }
        
        [StringLength(500)]
        public string? CertificateUrl { get; set; }
        
        [StringLength(100)]
        public string? IssuedBy { get; set; }
        
        public bool IsValid { get; set; } = true;
        
        [StringLength(1000)]
        public string? Description { get; set; }
        
        // Navigation Properties
        public virtual Course Course { get; set; } = null!;
        public virtual Employee Employee { get; set; } = null!;
    }
}