using System;
using ERMS.Domain.Common;
using ERMS.Domain.Entities.Identity;

namespace ERMS.Domain.Entities.Enterprise
{
    public class SubscriptionHistory : BaseEntity
    {
        public Guid EnterpriseId { get; set; }
        public Guid SubscriptionPlanId { get; set; }
        public string ActionType { get; set; } = null!; // Subscribe, Upgrade...
        public Guid? PreviousPlanId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "VND";
        public string? PaymentMethod { get; set; }
        public string? PaymentReference { get; set; }
        public DateTime PeriodStartDate { get; set; }
        public DateTime PeriodEndDate { get; set; }
        public string? Note { get; set; }
        public Guid? CreatedById { get; set; }

        public virtual Enterprise Enterprise { get; set; } = null!;
        public virtual SubscriptionPlan SubscriptionPlan { get; set; } = null!;
        public virtual SubscriptionPlan? PreviousPlan { get; set; }
    }
}
