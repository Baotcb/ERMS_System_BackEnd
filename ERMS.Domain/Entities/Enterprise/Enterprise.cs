using System;
using System.Collections.Generic;
using ERMS.Domain.Common;
using ERMS.Domain.Entities.Identity;

namespace ERMS.Domain.Entities.Enterprise
{
    public class Enterprise : BaseEntity
    {
        public string EnterpriseName { get; set; } = null!;
        public string EnterpriseCode { get; set; } = null!;
        public string? TaxCode { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? LogoUrl { get; set; }
        public Guid SubscriptionPlanId { get; set; }
        public DateTime SubscriptionStartDate { get; set; }
        public DateTime SubscriptionEndDate { get; set; }
        public string SubscriptionStatus { get; set; } = "Active";
        public Guid? CreatedById { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public virtual SubscriptionPlan SubscriptionPlan { get; set; } = null!;
        public virtual Identity.User? CreatedBy { get; set; }
        
        // Navigation properties for related data
        public virtual ICollection<Organization.Department> Departments { get; set; } = new List<Organization.Department>();
    }
}
