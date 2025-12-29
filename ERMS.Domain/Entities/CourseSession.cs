using ERMS.Domain.Common;
using System;
using System.Collections.Generic;

namespace ERMS.Domain.Entities
{
    public class CourseSession : BaseEntityInt
    {
        public Guid CourseId { get; set; }
        public Course Course { get; set; } = null!;

        public string Title { get; set; } = string.Empty;
        public string? Type { get; set; } // Video, LiveSession
        public string? VideoUrl { get; set; }
        public string? MeetingLink { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int OrderIndex { get; set; } = 1;

        // Navigation properties
        public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    }
}