using System;

namespace ERMS.Application.Features.Enterprises.DTOs
{
    public class EnterpriseDto
    {
        public Guid Id { get; set; }
        public string EnterpriseName { get; set; } = string.Empty;
        public string EnterpriseCode { get; set; } = string.Empty;
        public string? TaxCode { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? LogoUrl { get; set; }
        
        public Guid SubscriptionPlanId { get; set; }
        public string? SubscriptionPlanName { get; set; }
        public string? SubscriptionPlanCode { get; set; }
        
        public DateTime SubscriptionStartDate { get; set; }
        public DateTime SubscriptionEndDate { get; set; }
        public string SubscriptionStatus { get; set; } = "Active";
        
        public Guid? CreatedById { get; set; }
        public string? CreatedByName { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }

    public class PagedResponse<T>
    {
        public List<T> Data { get; set; } = new List<T>();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
