using System;
using System.Collections.Generic;

namespace ERMS.Domain.Entities
{
    public class Employee
    {
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public Guid EnterpriseId { get; set; }
        public Enterprise Enterprise { get; set; } = null!;

        public string EmployeeCode { get; set; } = string.Empty;
        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }

        public bool IsTrainer { get; set; } = false;
        public DateTime JoinDate { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public ICollection<JobPosting> CreatedJobPostings { get; set; } = new List<JobPosting>();
        public ICollection<Interview> Interviews { get; set; } = new List<Interview>();
        public ICollection<Course> CreatedCourses { get; set; } = new List<Course>();
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public ICollection<EnrollmentRequest> EnrollmentRequests { get; set; } = new List<EnrollmentRequest>();
        public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
        public ICollection<CourseFeedback> CourseFeedbacks { get; set; } = new List<CourseFeedback>();
        public ICollection<Report> GeneratedReports { get; set; } = new List<Report>();
    }
}