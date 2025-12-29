using ERMS.Domain.Common;
using System;

namespace ERMS.Domain.Entities
{
    public class Interview : BaseEntity
    {
        public Guid ApplicationId { get; set; }
        public Application Application { get; set; } = null!;

        public Guid InterviewerId { get; set; }
        public Employee Interviewer { get; set; } = null!;

        public DateTime ScheduledTime { get; set; }
        public string? MeetingLink { get; set; }
        public int RoundNumber { get; set; } = 1;
        public string? InterviewType { get; set; }
        public int? Score { get; set; } // 0-10
        public string? Result { get; set; } // Passed, Failed
        public string? Note { get; set; }
    }
}