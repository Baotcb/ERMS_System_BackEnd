using ERMS.Domain.Common;
using System;
using System.ComponentModel.DataAnnotations;

namespace ERMS.Domain.Entities
{
    public class Education : BaseEntity
    {
        public Guid CandidateId { get; set; }
        
        [Required]
        [StringLength(200)]
        public string SchoolName { get; set; } = string.Empty;
        
        [StringLength(50)]
        public string? Degree { get; set; } // HighSchool/Associate/Bachelor/Master/PhD/Other
        
        [StringLength(200)]
        public string? FieldOfStudy { get; set; }
        
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        
        public bool IsCurrent { get; set; } = false;
        
        [StringLength(20)]
        public string? Grade { get; set; }
        
        [StringLength(1000)]
        public string? Description { get; set; }
        
        // Navigation Properties
        public virtual Candidate Candidate { get; set; } = null!;
    }
}