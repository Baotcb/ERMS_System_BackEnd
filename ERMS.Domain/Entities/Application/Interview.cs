using System;
using System.Collections.Generic;
using ERMS.Domain.Common;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Enums;

namespace ERMS.Domain.Entities.Application
{
    public class Interview : BaseEntity
    {
        public Guid ApplicationId { get; set; }
        public string InterviewType { get; set; } = null!;
        public InterviewFormat InterviewFormat { get; set; } = InterviewFormat.Online;
        public int RoundNumber { get; set; } = 1;
        public DateTime ScheduledAt { get; set; }
        public int Duration { get; set; } = 60;
        public string? Location { get; set; }
        public string? MeetingLink { get; set; }
        public string Status { get; set; } = "Scheduled";
        public Guid ScheduledById { get; set; }
        public int? OverallRating { get; set; }
        public string? OverallFeedback { get; set; }
        public string? Decision { get; set; }
        public string? Note { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public virtual Application Application { get; set; } = null!;
        public virtual Identity.User ScheduledBy { get; set; } = null!;
        
        public virtual ICollection<InterviewParticipant> Participants { get; set; } = new List<InterviewParticipant>();
    }
}
