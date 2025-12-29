using ERMS.Domain.Common;
using System;

namespace ERMS.Domain.Entities
{
    public class Enrollment : BaseEntity
    {
        public Guid CourseId { get; set; }
        public Course Course { get; set; } = null!;

        public Guid EmployeeId { get; set; }
        public Employee Employee { get; set; } = null!;

        public int Progress { get; set; } = 0; // 0-100
        public string Status { get; set; } = "NotStarted"; // NotStarted, InProgress, Completed
        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    }
}