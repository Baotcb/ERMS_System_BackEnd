using ERMS.Domain.Common;
using System;
using System.ComponentModel.DataAnnotations;

namespace ERMS.Domain.Entities
{
    public class WorkExperience : BaseEntity
    {
        public Guid CandidateId { get; set; }
        
        [Required]
        [StringLength(200)]
        public string CompanyName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string Position { get; set; } = string.Empty;
        
        [StringLength(200)]
        public string? Location { get; set; }
        
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        
        public bool IsCurrent { get; set; } = false;
        
        [StringLength(2000)]
        public string? Description { get; set; }
        
        [StringLength(1000)]
        public string? Achievements { get; set; }
        
        // Navigation Properties
        public virtual Candidate Candidate { get; set; } = null!;
    }
}