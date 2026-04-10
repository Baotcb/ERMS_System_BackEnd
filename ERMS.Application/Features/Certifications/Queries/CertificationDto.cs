namespace ERMS.Application.Features.Certifications.Queries
{
    public class CertificationDto
    {
        public Guid EnrollmentId { get; set; }
        public Guid CourseId { get; set; }
        public string CourseName { get; set; } = null!;
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; } = null!;
        public string? CertificateUrl { get; set; }
        public DateTime? CertificateIssuedAt { get; set; }
        public int Progress { get; set; }
        public string Status { get; set; } = null!;
    }
}