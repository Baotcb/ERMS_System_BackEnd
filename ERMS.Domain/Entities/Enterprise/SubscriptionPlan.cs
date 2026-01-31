using System;
using System.Collections.Generic;
using ERMS.Domain.Common;

namespace ERMS.Domain.Entities.Enterprise
{
    public class SubscriptionPlan : BaseEntity
    {
        public string PlanName { get; set; } = null!;
        public string PlanCode { get; set; } = null!;
        public string? Description { get; set; }
        public int MaxUsers { get; set; } = 10;
        public int MaxJobPostings { get; set; } = 5;
        public int MaxCourses { get; set; } = 10;
        public decimal PriceMonthly { get; set; }
        public decimal PriceYearly { get; set; }
        public string? Features { get; set; } // JSON
        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public virtual ICollection<Enterprise> Enterprises { get; set; } = new List<Enterprise>();
    }
}
