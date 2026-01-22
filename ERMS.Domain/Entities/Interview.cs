using ERMS.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace ERMS.Domain.Entities
{
    public class Interview : BaseEntity
    {
        public Guid ApplicationId { get; set; }
        
        public int RoundNumber { get; set; } = 1;
        
        [Required]
        [StringLength(30)]
        public string InterviewType { get; set; } = string.Empty; // Screening/Technical/HR/Manager/Director/Culture/Final
        
        [StringLength(20)]
        public string InterviewMethod { get; set; } = "Online"; // Online/Offline
        
        public DateTime ScheduledAt { get; set; }
        public int Duration { get; set; } = 60; // minutes
        
        [StringLength(200)]
        public string? Location { get; set; }
        
        [StringLength(500)]
        public string? MeetingLink { get; set; }
        
        [StringLength(100)]
        public string? MeetingId { get; set; }
        
        [StringLength(50)]
        public string? MeetingPassword { get; set; }
        
        [StringLength(20)]
        public string Status { get; set; } = "Scheduled"; // Scheduled/Confirmed/InProgress/Completed/Cancelled/NoShow
        
        public bool CandidateConfirmed { get; set; } = false;
        public DateTime? CandidateConfirmedAt { get; set; }
        
        [StringLength(500)]
        public string? RescheduleReason { get; set; }
        public int RescheduleCount { get; set; } = 0;
        
        public int? Score { get; set; } // 1-10
        
        [StringLength(20)]
        public string? Result { get; set; } // Passed/Failed/OnHold
        
        public string? Feedback { get; set; } // NVARCHAR(MAX)
        
        [StringLength(1000)]
        public string? InternalNotes { get; set; }
        
        public Guid? SelectedById { get; set; }
        public Guid CreatedById { get; set; }
        
        // Navigation Properties
        public virtual Application Application { get; set; } = null!;
        public virtual User? SelectedBy { get; set; }
        public virtual User CreatedBy { get; set; } = null!;
        public virtual ICollection<InterviewParticipant> Participants { get; set; } = new HashSet<InterviewParticipant>();
    }
}