using System;
using ERMS.Domain.Common;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Constants.Enterprise;

namespace ERMS.Domain.Entities.Enterprise
{
    public class PaymentOrder : BaseEntity
    {
        public long OrderCode { get; set; }
        
        public Guid EnterpriseId { get; set; }
        public virtual Enterprise Enterprise { get; set; } = null!;

        public Guid SubscriptionPlanId { get; set; }
        public virtual SubscriptionPlan SubscriptionPlan { get; set; } = null!;

        public Guid? PreviousPlanId { get; set; }
        public virtual SubscriptionPlan? PreviousPlan { get; set; }

        public decimal Amount { get; set; }
        public string Currency { get; set; } = "VND";
        public string Description { get; set; } = "";
        public string Status { get; set; } = PaymentOrderConstants.Status.Pending;
        
        public string? PaymentLinkId { get; set; }
        public string? CheckoutUrl { get; set; }
        public string? PayOSReference { get; set; }
        
        public DateTime? PaidAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        
        public Guid CreatedById { get; set; }
        public virtual User CreatedBy { get; set; } = null!;
    }
}
