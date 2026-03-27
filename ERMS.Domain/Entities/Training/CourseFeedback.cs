using ERMS.Domain.Common;
using ERMS.Domain.Entities.Organization;
using System;
using System.Collections.Generic;
using System.Text;

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
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public virtual Course Course { get; set; } = null!;
        public virtual Employee Employee { get; set; } = null!;
    }
}
