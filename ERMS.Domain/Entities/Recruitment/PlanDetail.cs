using System;
using System.Collections.Generic;
using ERMS.Domain.Common;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Organization;

namespace ERMS.Domain.Entities.Recruitment
{
    public class PlanDetail : BaseEntity
    {
        public Guid RecruitmentPlanId { get; set; }
        public Guid RequestedById { get; set; }
        public string PositionTitle { get; set; } = null!;
        public int Quantity { get; set; } = 1;
        public string Priority { get; set; } = "Normal";
        public string? Justification { get; set; }
        public string? RequiredSkills { get; set; } // JSON
        public int? MinExperience { get; set; }
        public int? MaxExperience { get; set; }
        public string? EducationLevel { get; set; }
        public decimal? SalaryRangeMin { get; set; }
        public decimal? SalaryRangeMax { get; set; }
        public DateTime? ExpectedStartDate { get; set; }
        public string Status { get; set; } = "Draft";
        public Guid? ReviewerId { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewNote { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public virtual RecruitmentPlan RecruitmentPlan { get; set; } = null!;

        public virtual Identity.User RequestedBy { get; set; } = null!;
        public virtual Identity.User? Reviewer { get; set; }
        public virtual ICollection<JobPosting> JobPostings { get; set; } = new List<JobPosting>();
    }
}
