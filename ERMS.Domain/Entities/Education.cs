using ERMS.Domain.Common;
using System;

namespace ERMS.Domain.Entities
{
    public class Education : BaseEntityInt
    {
        public Guid ResumeId { get; set; }
        public Resume Resume { get; set; } = null!;

        public string SchoolName { get; set; } = string.Empty;
        public string? Degree { get; set; }
        public string? Major { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public double? Gpa { get; set; }
        public string? Status { get; set; } // Graduated, Studying
    }
}