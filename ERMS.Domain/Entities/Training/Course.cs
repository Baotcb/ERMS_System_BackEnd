using System;
using System.Collections.Generic;
using ERMS.Domain.Common;
using ERMS.Domain.Entities.Organization;

namespace ERMS.Domain.Entities.Training
{
    public class Course : BaseEntity
    {
        public Guid EnterpriseId { get; set; }
        public Guid? TrainingPlanId { get; set; }
        public string CourseName { get; set; } = null!;
        public string CourseCode { get; set; } = null!;
        public string? Description { get; set; }
        public string TrainerEmail { get; set; } = null!;
        public string? ContentManagerEmail { get; set; }
        public string? Location { get; set; }
        public DateTime StartTime { get; set; }
        public bool IsOnline { get; set; }
        public string? ThumbnailUrl { get; set; }
        public int? DurationMinutes { get; set; }
        public string? Level { get; set; }
        public string Status { get; set; } = "Draft";
        public bool IsMandatory { get; set; }
        public int? MaxEnrollments { get; set; }
        public DateTime? EnrollmentDeadline { get; set; }
        public DateTime? PublishedAt { get; set; }
        public string CompletionCriteria { get; set; } = "Quiz";
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public virtual Enterprise.Enterprise Enterprise { get; set; } = null!;
        public virtual TrainingPlan? TrainingPlan { get; set; }


        public virtual Quiz? Quiz { get; set; }
        public virtual ICollection<CourseSkill> CourseSkills { get; set; } = new List<CourseSkill>();
        public virtual ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
        public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public virtual ICollection<CourseFeedback> CourseFeedbacks { get; set; } = new List<CourseFeedback>();
        public virtual WorkshopConfirmation? WorkshopConfirmation { get; set; }
    }
}