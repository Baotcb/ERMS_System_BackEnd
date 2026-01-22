using ERMS.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace ERMS.Domain.Entities
{
    public class InterviewParticipant : BaseEntity
    {
        public Guid InterviewId { get; set; }
        public Guid UserId { get; set; }
        
        [Required]
        [StringLength(30)]
        public string Role { get; set; } = "Interviewer"; // MainInterviewer/Interviewer/Observer/HRSupport
        
        public bool IsRequired { get; set; } = true;
        public bool HasConfirmed { get; set; } = false;
        
        public int? Score { get; set; } // 1-10
        
        [StringLength(1000)]
        public string? Feedback { get; set; }
        
        // Navigation Properties
        public virtual Interview Interview { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}