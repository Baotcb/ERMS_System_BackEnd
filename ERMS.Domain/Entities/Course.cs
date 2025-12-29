using ERMS.Domain.Common;
using System;
using System.Collections.Generic;

namespace ERMS.Domain.Entities
{
    public class Course : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ThumbnailUrl { get; set; }

        public Guid CreatorId { get; set; }
        public Employee Creator { get; set; } = null!;

        public bool IsRequired { get; set; } = false;
        public int MinAttendancePercent { get; set; } = 80;
        public string Status { get; set; } = "Draft"; // Draft, Published, Archived

        // Navigation properties
        public ICollection<CourseSkill> CourseSkills { get; set; } = new List<CourseSkill>();
        public ICollection<CourseSession> CourseSessions { get; set; } = new List<CourseSession>();
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public ICollection<EnrollmentRequest> EnrollmentRequests { get; set; } = new List<EnrollmentRequest>();
        public ICollection<CourseFeedback> CourseFeedbacks { get; set; } = new List<CourseFeedback>();
    }
}