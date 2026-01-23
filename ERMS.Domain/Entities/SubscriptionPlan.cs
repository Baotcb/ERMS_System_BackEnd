using ERMS.Domain.Common;
using System;
using System.Collections.Generic;

namespace ERMS.Domain.Entities
{
    public class SubscriptionPlan : BaseEntity
    {
        public string PlanName { get; set; } = string.Empty;
        public string PlanCode { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int MaxUsers { get; set; } = 10;
        public int MaxJobPostings { get; set; } = 5;
        public int MaxCourses { get; set; } = 10;
        public decimal PriceMonthly { get; set; } = 0;
        public decimal PriceYearly { get; set; } = 0;
        public string? Features { get; set; } // JSON array of feature flags
        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public ICollection<Enterprise> Enterprises { get; set; } = new List<Enterprise>();
    }
}
