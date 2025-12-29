using ERMS.Domain.Common;
using System;

namespace ERMS.Domain.Entities
{
    public class CourseFeedback : BaseEntityInt
    {
        public Guid CourseId { get; set; }
        public Course Course { get; set; } = null!;

        public Guid EmployeeId { get; set; }
        public Employee Employee { get; set; } = null!;

        public int Rating { get; set; } // 1-5
        public string? Comment { get; set; }
    }
}