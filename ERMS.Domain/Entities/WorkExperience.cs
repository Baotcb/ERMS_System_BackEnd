using ERMS.Domain.Common;
using System;

namespace ERMS.Domain.Entities
{
    public class WorkExperience : BaseEntityInt
    {
        public Guid ResumeId { get; set; }
        public Resume Resume { get; set; } = null!;

        public string CompanyName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Description { get; set; }
    }
}