using ERMS.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace ERMS.Domain.Entities
{
    public class SubscriptionHistory : BaseEntity
    {
        public Guid EnterpriseId { get; set; }
        public Guid SubscriptionPlanId { get; set; }
        
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        
        public decimal Amount { get; set; }
        
        [StringLength(50)]
        public string? PaymentMethod { get; set; } // BankTransfer/Card/...
        
        [StringLength(20)]
        public string PaymentStatus { get; set; } = "Pending"; // Pending/Paid/Failed
        
        [StringLength(100)]
        public string? TransactionId { get; set; }
        
        [StringLength(500)]
        public string? InvoiceUrl { get; set; }
        
        [StringLength(500)]
        public string? Notes { get; set; }
        
        // Navigation Properties
        public virtual Enterprise Enterprise { get; set; } = null!;
        public virtual SubscriptionPlan SubscriptionPlan { get; set; } = null!;
    }
}