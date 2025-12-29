using ERMS.Domain.Common;
using System;

namespace ERMS.Domain.Entities
{
    public class EnrollmentRequest : BaseEntityInt
    {
        public Guid EmployeeId { get; set; }
        public Employee Employee { get; set; } = null!;

        public Guid CourseId { get; set; }
        public Course Course { get; set; } = null!;

        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        public Guid? ReviewerId { get; set; }
        public Employee? Reviewer { get; set; }

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    }
}