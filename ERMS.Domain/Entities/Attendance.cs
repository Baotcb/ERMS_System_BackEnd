using ERMS.Domain.Common;
using System;

namespace ERMS.Domain.Entities
{
    public class Attendance : BaseEntityInt
    {
        public int SessionId { get; set; }
        public CourseSession Session { get; set; } = null!;

        public Guid EmployeeId { get; set; }
        public Employee Employee { get; set; } = null!;

        public string? Status { get; set; } // Present, Absent, Late

        public Guid? RecordedBy { get; set; }
        public Employee? Recorder { get; set; }

        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
        public string? Note { get; set; }
    }
}