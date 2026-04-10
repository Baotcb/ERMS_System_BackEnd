namespace ERMS.Application.Features.Enrollments.Queries.GetCourseAttendance
{
    public class CourseAttendanceDto
    {
        public Guid EnrollmentId { get; set; }
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; } = null!;
        public string EmployeeCode { get; set; } = null!;
        public string Status { get; set; } = null!;
        public int Progress { get; set; }
    }
}