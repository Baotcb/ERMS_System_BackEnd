using System;
using System.Collections.Generic;
using ERMS.Domain.Common;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Organization;

namespace ERMS.Domain.Entities.Recruitment
{
    public class JobPosting : BaseEntity
    {
        public Guid EnterpriseId { get; set; }
        public Guid? PlanDetailId { get; set; }
        public int DepartmentId { get; set; }
        public string JobTitle { get; set; } = null!;
        public string? JobCode { get; set; }
        public string Description { get; set; } = null!;
        public string? Requirements { get; set; }
        public string? Benefits { get; set; }
        public string EmploymentType { get; set; } = "FullTime";
        public string? ExperienceLevel { get; set; }
        public string? EducationLevel { get; set; }
        public decimal? SalaryRangeMin { get; set; }
        public decimal? SalaryRangeMax { get; set; }
        public bool ShowSalary { get; set; }
        public string? Location { get; set; }
        public string? RemoteOption { get; set; }
        public int Quantity { get; set; } = 1;
        public DateTime? ApplicationDeadline { get; set; }
        public string Status { get; set; } = "Draft";
        public DateTime? PublishedAt { get; set; }
        public Guid? PublishedById { get; set; }
        public DateTime? ClosedAt { get; set; }
        public int ViewCount { get; set; }
        public int ApplicationCount { get; set; }
        public Guid CreatedById { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public virtual Enterprise.Enterprise Enterprise { get; set; } = null!;
        public virtual PlanDetail? PlanDetail { get; set; }
        public virtual Department Department { get; set; } = null!;
        public virtual Identity.User? PublishedBy { get; set; }
        public virtual Identity.User CreatedBy { get; set; } = null!;
        
        public virtual ICollection<JobSkill> JobSkills { get; set; } = new List<JobSkill>();
        public virtual ICollection<Application.Application> Applications { get; set; } = new List<Application.Application>();
    }
}
