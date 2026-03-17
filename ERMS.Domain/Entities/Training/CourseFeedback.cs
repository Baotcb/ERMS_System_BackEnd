using System;
using ERMS.Domain.Common;
using ERMS.Domain.Entities.Organization;

namespace ERMS.Domain.Entities.Training
{
    public class CourseFeedback : BaseEntity
    {
        public Guid CourseId { get; set; }
        public Guid EmployeeId { get; set; }
        public int CourseRating { get; set; }
        public int TrainerRating { get; set; }
        public string? Comment { get; set; }
        public bool IsAnonymous { get; set; }

        public virtual Course Course { get; set; } = null!;
        public virtual Employee Employee { get; set; } = null!;
    }
}
