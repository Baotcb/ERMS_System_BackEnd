using ERMS.Domain.Common;
using System;
using System.Collections.Generic;

namespace ERMS.Domain.Entities
{
    public class Enterprise : BaseEntity
    {
        public string EnterpriseName { get; set; } = string.Empty;
        public string EnterpriseCode { get; set; } = string.Empty;
        public string? TaxCode { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? LogoUrl { get; set; }
        
        public Guid SubscriptionPlanId { get; set; }
        public SubscriptionPlan SubscriptionPlan { get; set; } = null!;
        
        public DateTime SubscriptionStartDate { get; set; }
        public DateTime SubscriptionEndDate { get; set; }
        public string SubscriptionStatus { get; set; } = "Active"; // Active, Expired, Cancelled, Trial, PastDue
        
        public Guid? CreatedById { get; set; }
        public User? CreatedBy { get; set; }
        
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<Department> Departments { get; set; } = new List<Department>();
        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
        public ICollection<JobPosting> JobPostings { get; set; } = new List<JobPosting>();
        public ICollection<Skill> Skills { get; set; } = new List<Skill>();
        public ICollection<Course> Courses { get; set; } = new List<Course>();
    }
}
