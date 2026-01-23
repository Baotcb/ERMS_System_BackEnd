using System;
using ERMS.Domain.Common;
using ERMS.Domain.Entities.Organization;

namespace ERMS.Domain.Entities.Application
{
    public class InterviewParticipant : BaseEntity
    {
        public Guid InterviewId { get; set; }
        public Guid EmployeeId { get; set; }
        public string Role { get; set; } = "Interviewer";
        public bool IsRequired { get; set; } = true;
        public string ConfirmationStatus { get; set; } = "Pending";
        public int? Rating { get; set; }
        public string? Feedback { get; set; }
        public string? Recommendation { get; set; }
        public DateTime? FeedbackSubmittedAt { get; set; }

        public virtual Interview Interview { get; set; } = null!;
        public virtual Employee Employee { get; set; } = null!;
    }
}
